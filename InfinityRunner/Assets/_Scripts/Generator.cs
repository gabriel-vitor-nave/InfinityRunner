using System.Collections;
using UnityEngine;

public class Generator : MonoBehaviour
{
    [Header("Prefabs dos segmentos")]
    public GameObject[] segmentos;

    [Header("Configuração de spawn")]
    [SerializeField] private float zPos = 50f;
    [SerializeField] private float spawnDelay = 3f;
    [SerializeField] private bool gerarSegmentos = false;

    private void Update()
    {
        if (!gerarSegmentos)
        {
            gerarSegmentos = true;
            StartCoroutine(SegmentGen());
        }
    }

    private IEnumerator SegmentGen()
    {
        int indiceSegmento = Random.Range(0, segmentos.Length);

        Instantiate(segmentos[indiceSegmento], new Vector3(0f, 0f, zPos), Quaternion.identity);
        zPos += 50f;

        yield return new WaitForSeconds(spawnDelay);
        gerarSegmentos = false;
    }
}