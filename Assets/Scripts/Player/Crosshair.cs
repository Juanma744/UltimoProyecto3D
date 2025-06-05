using UnityEngine;

public class SimpleCrosshair : MonoBehaviour
{
    public float size = 8f;
    public Color color = Color.white;

    void OnGUI()
    {
        // Guardar el color original
        Color oldColor = GUI.color;
        GUI.color = color;

        float xMin = (Screen.width / 2) - (size / 2);
        float yMin = (Screen.height / 2) - (size / 2);
        GUI.DrawTexture(new Rect(xMin, yMin, size, size), Texture2D.whiteTexture);

        // Restaurar el color original
        GUI.color = oldColor;
    }
}
