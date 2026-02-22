using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldEntityManager : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] GameObject startGate, DayLight, NightLight;
    [SerializeField] Material lightsRed, lightsGreen, lightsYellow, skyboxMaterial;
    [SerializeField] Texture2D daySkybox, nightSkybox;
    
    // Start is called before the first frame update
    void Start()
    {
        PickRandomSkybox();
    }
    void PickRandomSkybox(){
        int r = Random.Range(0, 2);
        if(r == 0){
            skyboxMaterial.SetTexture("_MainTex", daySkybox);
            DayLight.SetActive(true);
            NightLight.SetActive(false);
        }
        else{
            skyboxMaterial.SetTexture("_MainTex", nightSkybox);
            DayLight.SetActive(false);
            NightLight.SetActive(true);
        }
    }
    public void PlayStartSpawnAnimation(){
        StartCoroutine(StartGateSpawnAnimation());
    }

    IEnumerator StartGateSpawnAnimation(){
        mainCamera.GetComponent<Animation>().Play("StartCamera");
        startGate.SetActive(true);
        yield return new WaitForSeconds(1f);
        MeshRenderer startGateRenderer = startGate.GetComponent<MeshRenderer>();
        Material m = startGateRenderer.sharedMaterials[0];
        float t = 0, length = 5;
        while(t < length){
            t += Time.deltaTime;
            m.SetFloat("_MainHeight", Mathf.Lerp(0, 1, t/length) - 0.25f);
            m.SetFloat("_EdgeHeight", Mathf.Lerp(0, 1, t/length));
            yield return new WaitForEndOfFrame();
        }
        //Debug.Log("Start Animation Finished");
        Material[] startGateMaterials = startGateRenderer.materials;
        yield return new WaitForSeconds(0.2f);
        // Red light
        startGateMaterials[1] = lightsRed;
        startGateRenderer.materials = startGateMaterials;
        yield return new WaitForSeconds(0.2f);

        // Yellow light
        startGateMaterials[2] = lightsYellow;
        startGateRenderer.materials = startGateMaterials;
        yield return new WaitForSeconds(0.2f);

        // Green light
        startGateMaterials[3] = lightsGreen;
        startGateRenderer.materials = startGateMaterials;
    }
}
