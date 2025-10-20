using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public class ModificadorDeObjetosUI : MonoBehaviour
{
    public TMP_InputField campoRocas;      // Campo para el valor de "Rocas"
    public TMP_InputField campoParedes;     // Campo para el valor de "Paredes"

    public LevelRunner levelRunnerScript; 
    
    public void AplicarCambios()
    {

        string textoRocas = campoRocas.text;
        string textoParedes = campoParedes.text;
        if (int.TryParse(textoRocas, out int numRocas) && int.TryParse(textoParedes, out int numParedes))
        {


            levelRunnerScript.numRocksAndHoles = numRocas;
            levelRunnerScript.numWalls = numParedes;
            
            Debug.Log($"Variables modificadas en LevelRunner. Rocas/Agujeros: {numRocas}, Paredes: {numParedes}");

            levelRunnerScript.GenerateLevel();
        }

    }
}