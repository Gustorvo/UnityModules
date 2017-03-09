﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Leap.Unity;
using Leap.Unity.Query;

public class LeapGuiGroup : LeapGuiComponentBase<LeapGui> {

  [SerializeField, HideInInspector]
  private LeapGui _gui;

  [SerializeField]
  private LeapGuiRendererBase _renderer;

  [SerializeField]
  private List<LeapGuiFeatureBase> _features = new List<LeapGuiFeatureBase>();

  [SerializeField, HideInInspector]
  private List<LeapGuiElement> _elements = new List<LeapGuiElement>();

  [SerializeField, HideInInspector]
  private List<SupportInfo> _supportInfo = new List<SupportInfo>();

  [SerializeField, HideInInspector]
  private bool _addRemoveSupported;

  #region PRIVATE VARIABLES
  private List<LeapGuiElement> _toAdd = new List<LeapGuiElement>();
  private List<LeapGuiElement> _toRemove = new List<LeapGuiElement>();
  #endregion

  #region PUBLIC API

  public LeapGui gui {
    get {
      return _gui;
    }
  }

#if UNITY_EDITOR
  public new LeapGuiRendererBase renderer {
#else
  public LeapGuiRendererBase renderer {
#endif
    get {
      return _renderer;
    }
  }

  public List<LeapGuiFeatureBase> features {
    get {
      return _features;
    }
  }

  public List<LeapGuiElement> elements {
    get {
      return _elements;
    }
  }

  /// <summary>
  /// Maps 1-to-1 with the feature list, where each element represents the
  /// support that feature currently has.
  /// </summary>
  public List<SupportInfo> supportInfo {
    get {
      return _supportInfo;
    }
  }

  public bool addRemoveSupported {
    get {
      return _addRemoveSupported;
    }
  }

  public void Init(LeapGui gui, Type rendererType) {
    AssertHelper.AssertEditorOnly();
    Assert.IsNotNull(gui);
    Assert.IsNotNull(rendererType);
    _gui = gui;

    _renderer = gameObject.AddComponent(rendererType) as LeapGuiRendererBase;
    Assert.IsNotNull(_renderer);
    _renderer.gui = _gui;
    _renderer.group = this;
    _renderer.OnEnableRendererEditor();
  }

  /// <summary>
  /// Tries to add a new gui element to this gui at runtime.
  /// Element is not actually added until the next gui cycle.
  /// </summary>
  public bool TryAddElement(LeapGuiElement element) {
    AssertHelper.AssertRuntimeOnly();
    Assert.IsNotNull(element);
    //TO WHICH GROUP AAAA

    _toAdd.Add(element);
    return true;
  }

  /// <summary>
  /// Tries to remove a gui element from this gui at runtime.
  /// Element is not actually removed until the next gui cycle.
  /// </summary>
  public bool TryRemoveElement(LeapGuiElement element) {
    AssertHelper.AssertRuntimeOnly();
    Assert.IsNotNull(element);
    //TO WHICH GROUP AAAA

    _toRemove.Add(element);
    return true;
  }

  public bool GetSupportedFeatures<T>(List<T> features) where T : LeapGuiFeatureBase {
    features.Clear();
    for (int i = 0; i < _features.Count; i++) {
      var feature = _features[i];
      if (!(feature is T)) continue;
      if (_supportInfo[i].support == SupportType.Error) continue;

      features.Add(feature as T);
    }

    return features.Count != 0;
  }

  public void AddElements(List<LeapGuiElement> elements) {
    using (new ProfilerSample("Add Elements")) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        foreach (var element in elements) {
          _elements.Add(element);

          Transform t = element.transform;
          while (true) {
            var anchor = t.GetComponent<AnchorOfConstantSize>();
            if (anchor != null && anchor.enabled) {
              element.OnAttachedToGui(this, t);
              break;
            }

            t = t.parent;

            if (t = transform) {
              element.OnAttachedToGui(this, transform);
              break;
            }
          }
        }

        _gui.ScheduleEditorUpdate();
        return;
      }
#endif
      //TODO: runtime logic for addition
    }
  }

  public void RemoveElements(List<LeapGuiElement> elements) {
    using (new ProfilerSample("Remove Elements")) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        foreach (var element in elements) {
          element.OnDetachedFromGui();
          _elements.Remove(element);
        }
        _gui.ScheduleEditorUpdate();
        return;
      }
#endif
      //TODO: runtime logic for removal
    }
  }

  public void AddFeature(Type featureType) {
    AssertHelper.AssertEditorOnly();
    _gui.ScheduleEditorUpdate();

    var feature = gameObject.AddComponent(featureType);
    _features.Add(feature as LeapGuiFeatureBase);
  }

  public void CollectUnattachedElements() {
    using (new ProfilerSample("Rebuild Element List")) {
      collectUnattachedElementsRecursively(_gui.transform, _gui.transform);
    }
  }

  public void RebuildFeatureData() {
    using (new ProfilerSample("Rebuild Feature Data")) {
      foreach (var feature in _features) {
        feature.ClearDataObjectReferences();
        feature.isDirty = true;
      }

      for (int i = 0; i < _elements.Count; i++) {
        var element = _elements[i];

        List<LeapGuiElementData> dataList = new List<LeapGuiElementData>();
        foreach (var feature in _features) {
          var dataObj = element.data.Query().OfType(feature.GetDataObjectType()).FirstOrDefault();
          if (dataObj != null) {
            element.data.Remove(dataObj);
          } else {
            dataObj = feature.CreateDataObject(element);
          }
          feature.AddDataObjectReference(dataObj);
          dataList.Add(dataObj);
        }

        foreach (var dataObj in element.data) {
          DestroyImmediate(dataObj);
        }

        element.OnAssignFeatureData(dataList);
      }

      //Could be more efficient
      foreach (var feature in _features) {
        feature.AssignFeatureReferences();
      }
    }
  }

  public void RebuildFeatureSupportInfo() {
    using (new ProfilerSample("Rebuild Support Info")) {
      var typeToFeatures = new Dictionary<Type, List<LeapGuiFeatureBase>>();
      foreach (var feature in _features) {
        Type featureType = feature.GetType();
        List<LeapGuiFeatureBase> list;
        if (!typeToFeatures.TryGetValue(featureType, out list)) {
          list = new List<LeapGuiFeatureBase>();
          typeToFeatures[featureType] = list;
        }

        list.Add(feature);
      }


      var featureToInfo = new Dictionary<LeapGuiFeatureBase, SupportInfo>();

      foreach (var pair in typeToFeatures) {
        var featureType = pair.Key;
        var featureList = pair.Value;
        var infoList = new List<SupportInfo>().FillEach(featureList.Count, () => SupportInfo.FullSupport());

        var castList = Activator.CreateInstance(typeof(List<>).MakeGenericType(featureType)) as IList;
        foreach (var feature in featureList) {
          castList.Add(feature);
        }

        try {
          if (_renderer == null) continue;

          var interfaceType = typeof(ISupportsFeature<>).MakeGenericType(featureType);
          if (!interfaceType.IsAssignableFrom(_renderer.GetType())) {
            infoList.FillEach(() => SupportInfo.Error("This renderer does not support this feature."));
            continue;
          }

          var supportDelegate = interfaceType.GetMethod("GetSupportInfo");

          if (supportDelegate == null) {
            Debug.LogError("Could not find support delegate.");
            continue;
          }

          supportDelegate.Invoke(_renderer, new object[] { castList, infoList });
        } finally {
          for (int i = 0; i < featureList.Count; i++) {
            featureToInfo[featureList[i]] = infoList[i];
          }
        }
      }

      _supportInfo = new List<SupportInfo>();
      foreach (var feature in _features) {
        _supportInfo.Add(feature.GetSupportInfo(this).OrWorse(featureToInfo[feature]));
      }
    }
  }

  public void UpdateRendererEditor(bool heavyRebuild) {
    _renderer.OnUpdateRendererEditor(heavyRebuild);

    foreach (var feature in _features) {
      feature.isDirty = false;
    }
  }

  public void UpdateRenderer() {
    _renderer.OnUpdateRenderer();
  }

  #endregion

  #region UNITY CALLBACKS

  protected override void OnValidate() {
    base.OnValidate();

    if (_gui == null) {
      _gui = GetComponent<LeapGui>();
    }

    if (!Application.isPlaying) {
      _addRemoveSupported = true;
      if (_renderer != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(renderer.GetType());
      }
      if (_gui.space != null) {
        _addRemoveSupported &= typeof(ISupportsAddRemove).IsAssignableFrom(_gui.space.GetType());
      }
    }

    for (int i = _features.Count; i-- != 0;) {
      if (_features[i] == null) {
        _features.RemoveAt(i);
      }
    }

    if (_renderer != null) {
      _renderer.gui = _gui;
      _renderer.group = this;
    }
  }

  private void OnDestroy() {
    if (_renderer != null) DestroyImmediate(_renderer);

    foreach (var feature in _features) {
      DestroyImmediate(feature);
    }
  }

  #endregion

  #region PRIVATE IMPLEMENTATION
  private void collectUnattachedElementsRecursively(Transform root, Transform currAnchor) {
    int count = root.childCount;
    for (int i = 0; i < count; i++) {
      Transform child = root.GetChild(i);
      if (!child.gameObject.activeSelf) continue;

      var childAnchor = currAnchor;

      var anchorComponent = child.GetComponent<AnchorOfConstantSize>();
      if (anchorComponent != null && anchorComponent.enabled) {
        childAnchor = anchorComponent.transform;
      }

      var element = _renderer.GetValidElementOnObject(child.gameObject);
      if (!element.IsAttachedToGroup) {
        if (element != null && element.enabled) {
          element.OnAttachedToGui(this, childAnchor);
          _elements.Add(element);
        }
      }

      collectUnattachedElementsRecursively(child, childAnchor);
    }
  }
  #endregion
}
