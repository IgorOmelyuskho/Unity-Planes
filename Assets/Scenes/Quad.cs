using System;
using UnityEngine;

public class Quad : MonoBehaviour
{
    public Renderer rend;
    public Texture2D texture;
    int textureW = 1500 * 2;
    int textureH = 3000 * 2;

    // Start is called before the first frame update
    void Start()
    {
        Init(100, 100);
    }

    void Init(float xCoord, float yCoord)
    {
        float xS = 8;
        float yS = 8 * (textureH / textureW);
        float zS = 1;
        transform.localScale = new Vector3(xS, yS, zS);
        rend = GetComponent<Renderer>();
        texture = new Texture2D(textureW, textureH);

        Color32[] newColors = new Color32[texture.width * texture.height];
        texture.SetPixels32(newColors);

        for (var i = 0; i < Shared.angleArr.Length; i++)
        {
            for (var j = 0; j < Shared.angleArr[i].coordsArr.Length; j++)
            {
                float x = Shared.angleArr[i].coordsArr[j].x;
                float y = Shared.angleArr[i].coordsArr[j].y - texture.height / 2;
                //texture.SetPixel(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Color.blue);
                DrawDot(x, y, Color.blue);
            }
        }

        DrawBigDot(xCoord, yCoord - texture.height / 2, new Color(1, 0, 0.4f));
        NearestAnglIndexAndCoordIndex nearest = Shared.FindNearestIndex(xCoord, yCoord);
        DrawForAngle(new Color(0.7f, 0.5f, 0.1f), nearest.interpolateDegAngle);
        DrawAnotherColor(new Color(1f, 0.1f, 1f), nearest.angleIndex);
        DrawAnotherColor(new Color(1f, 0.1f, 1f), nearest.angleIndex2);
        texture.Apply();
        rend.material.mainTexture = texture;
    }

    void DrawForAngle(Color color, float degAngle)
    {
        Vector2[] res = Shared.CalcAnyAngle(degAngle);
        for (var i = 0; i < res.Length; i++)
        {
            res[i].y = res[i].y - texture.height / 2;
            //texture.SetPixel(Mathf.RoundToInt(res[i].x), Mathf.RoundToInt(res[i].y), color);
            DrawDot(res[i].x, res[i].y, color);
        }
        texture.Apply();
    }


    void DrawAnotherColor(Color color, int index)
    {
        for (var i = 0; i < Shared.angleArr[index].coordsArr.Length; i++)
        {
            float xCoord = Shared.angleArr[index].coordsArr[i].x;
            float yCoord = Shared.angleArr[index].coordsArr[i].y - texture.height / 2;
            //texture.SetPixel(Mathf.RoundToInt(xCoord), Mathf.RoundToInt(yCoord), color);
            DrawDot(xCoord, yCoord, color);
        }
        texture.Apply();
    }

    void DrawDot(float x, float y, Color color)
    {
        int xInt = Mathf.RoundToInt(x);
        int yInt = Mathf.RoundToInt(y);
        texture.SetPixel(xInt, yInt, color);
        texture.SetPixel(xInt - 1, yInt - 1, color);
        texture.SetPixel(xInt - 1, yInt + 1, color);
        texture.SetPixel(xInt + 1, yInt - 1, color);
        texture.SetPixel(xInt + 1, yInt + 1, color);
        texture.SetPixel(xInt, yInt - 1, color);
        texture.SetPixel(xInt, yInt + 1, color);
        texture.SetPixel(xInt - 1, yInt, color);
        texture.SetPixel(xInt + 1, yInt, color);
    }

    void DrawBigDot(float x, float y, Color color)
    {
        int c = 4;
        DrawDot(x, y, color);
        DrawDot(x - c, y - c, color);
        DrawDot(x - c, y + c, color);
        DrawDot(x + c, y - c, color);
        DrawDot(x + c, y + c, color);
        DrawDot(x, y - c, color);
        DrawDot(x, y + c, color);
        DrawDot(x - c, y, color);
        DrawDot(x + c, y, color);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            RaycastHit hit;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                return;

            Renderer rend = hit.transform.GetComponent<Renderer>();
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
                return;

            Texture2D tex = rend.material.mainTexture as Texture2D;
            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x *= tex.width;
            pixelUV.y *= tex.height;

            Init(pixelUV.x, pixelUV.y - texture.height / 2);
        }
    }
}
