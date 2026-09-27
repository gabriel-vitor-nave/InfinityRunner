using UnityEngine;

// este código estará anexado ao Player (GameObject)
public class PlayerMovement : MonoBehaviour
{
    public float velocidadePlayer = 2f;
    public float velocidadeHorizontal = 3f;
    public float rightLimit = 5.5f;
    public float leftLimit = -5.5f;


    void Update()
    {
        // velocidade do player relativa a o tempó
        transform.Translate(Vector3.forward * Time.deltaTime * velocidadePlayer, Space.World);
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (transform.position.x > leftLimit)
            {
                transform.Translate(Vector3.left * Time.deltaTime * velocidadeHorizontal, Space.World);
            }
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (transform.position.x < rightLimit)
            {
                transform.Translate(Vector3.right * Time.deltaTime * velocidadeHorizontal, Space.World);
            }
        }
    }
}