using UnityEngine;
using UnityEngine.UIElements;

public class JoinGameScreen: VisualElement
{
    public new class UxmlFactory : UxmlFactory<JoinGameScreen, UxmlTraits> { }

    public JoinGameScreen()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    void OnGeometryChanged(GeometryChangedEvent evt)
    {
        this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }
}
