using UnityEngine;
using UnityEngine.UIElements;

public class ManualConnectScreen: VisualElement
{
    public new class UxmlFactory : UxmlFactory<ManualConnectScreen, UxmlTraits> { }

    public ManualConnectScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
}
