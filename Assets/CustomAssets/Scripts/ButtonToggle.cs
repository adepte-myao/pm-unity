using UnityEngine;
using UnityEngine.UI;

public class ButtonToggle : MonoBehaviour
{
    private Button button;
    private Camera targetCamera;
    private bool hasComponent;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(UpdatePeerState);

        targetCamera = Camera.main;
        if (targetCamera == null )
        {
            Debug.LogError("camera object is null");
        }

        hasComponent = false;
    }

    void UpdatePeerState()
    {
        Debug.Log("updating peer state");
        if (hasComponent && Application.isEditor)
        {
            DestroyImmediate(targetCamera.gameObject.GetComponent<RTCCapture>());
        }
        else if (hasComponent)
        {
            Destroy(targetCamera.gameObject.GetComponent<RTCCapture>());
        }
        else
        {
            targetCamera.gameObject.AddComponent<RTCCapture>();
        }

        hasComponent = !hasComponent;
    }

    void Update()
    {
        
    }
}
