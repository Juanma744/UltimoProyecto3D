using UnityEngine;
using UnityEngine.SceneManagement;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public DungeonGenerator dungeonGenerator; // Arrástralo desde el inspector  

    public void CargarNivel(string nombreNivel)
    {
        SceneManager.LoadScene(nombreNivel);
        Debug.Log("Cargando nivel: " + nombreNivel);
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego...");
    }

    public void IrAEscenaDungeon()
    {
        SceneManager.LoadScene("Snake Dungeon");
        Debug.Log("Cargando escena de dungeon...");
    }



}
