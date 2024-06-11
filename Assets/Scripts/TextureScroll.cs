using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureScroll : MonoBehaviour
{
    [SerializeField] Material material;
    [SerializeField] float scrollSpeed = 0.5f;

    // Update is called once per frame
    void Update()
    {
        material.mainTextureOffset += new Vector2(scrollSpeed * Time.deltaTime, 0);
    }
}
