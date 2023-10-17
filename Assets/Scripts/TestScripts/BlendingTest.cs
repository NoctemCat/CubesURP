using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendingTest : MonoBehaviour
{
    public GameObject red;
    public GameObject green;
    public GameObject blue;
    public GameObject blend;
    public Vector3 blendPos;
    public bool refresh;

    void Start()
    {

    }

    private void OnValidate()
    {
        blend.transform.position = blendPos;

        MeshRenderer renderer = blend.GetComponent<MeshRenderer>();

        float redDst = 1 / (red.transform.position - blend.transform.position).sqrMagnitude;
        float greenDst = 1 / (green.transform.position - blend.transform.position).sqrMagnitude;
        float blueDst = 1 / (blue.transform.position - blend.transform.position).sqrMagnitude;

        float redMult = redDst / (redDst + greenDst + blueDst);
        float greenMult = greenDst / (redDst + greenDst + blueDst);
        float blueMult = blueDst / (redDst + greenDst + blueDst);

        Vector3 redColor = new(1f, 0, 0);
        Vector3 greenColor = new(0, 1f, 0);
        Vector3 blueColor = new(0, 0f, 1f);

        redColor *= redMult;
        greenColor *= greenMult;
        blueColor *= blueMult;

        Vector3 sum = redColor + greenColor + blueColor;

        renderer.sharedMaterial.color = new Color(sum.x, sum.y, sum.z);
    }
}
