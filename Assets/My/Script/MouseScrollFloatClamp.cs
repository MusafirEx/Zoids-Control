using UnityEngine;

public class MouseScrollFloatClamp : MonoBehaviour
{
    [Header("Value Settings")]
    public float value = 1.0f;
    public float minValue = 0.5f;
    public float maxValue = 2.0f;
    public RectTransform Map;

    [Header("Scroll Settings")]
    public float scrollSpeed = 0.1f;

    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (scroll != 0f)
        {
            value += scroll * scrollSpeed;
            value = Mathf.Clamp(value, minValue, maxValue);

            Map.localScale=new Vector3(value, value,0);
            Debug.Log("Current Value: " + value);
        }
    }
}