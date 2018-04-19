using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteEditorScript : MonoBehaviour {

    SpriteRenderer spriteRenderer;
    Sprite sprite;

	// Use this for initialization
	void Start () {
        spriteRenderer = GetComponent<SpriteRenderer>();

        sprite = spriteRenderer.sprite;

        Debug.Log("We have reference to sprite " + sprite.name);
        Debug.Log("It's border is " + sprite.border);
        Debug.Log("It's RECT is " + sprite.rect);
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.X))
        {
            Color32[] pix32 = sprite.texture.GetPixels32();
            /*Color[] pix = sprite.texture.GetPixels();
            Debug.Log("The first pixel is " + pix[0]);
            Debug.Log("The second pixel is " + pix[1]);
            Debug.Log("The third pixel is " + pix[2]);
            Debug.Log("The first 32pixel is " + pix32[0]);
            Debug.Log("The second 32pixel is " + pix32[1]);
            Debug.Log("The third 32pixel is " + pix32[2]);
            pix[0] = Color.white;
            */
            sprite.texture.SetPixels32(pix32);
            //sprite.texture.SetPixels(pix);
            sprite.texture.Apply();
        }
	}
}
