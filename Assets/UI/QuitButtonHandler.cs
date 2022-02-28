using UnityEngine;
using UnityEngine.UIElements;

public class QuitButtonHandler : MonoBehaviour
{
    public UIDocument m_TitleUIDocument;
    private VisualElement m_titleScreenManagerVE;

    void OnEnable()
    {
        m_titleScreenManagerVE = m_TitleUIDocument.rootVisualElement;
        m_titleScreenManagerVE.Q("quit-button")?.RegisterCallback<ClickEvent>(ev => ClickedQuit());
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void ClickedQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
