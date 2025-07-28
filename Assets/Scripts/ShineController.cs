using UnityEngine;

public class ShineController : MonoBehaviour
{
    public Material targetMaterial;
    public float speed = 0.5f;
    private float offset = -1f;

    void Update()
    {
        offset += Time.deltaTime * speed;

        if (offset > 1.5f)
            offset = -1f;

        targetMaterial.SetFloat("_ShineOffset", offset);
    }
}
