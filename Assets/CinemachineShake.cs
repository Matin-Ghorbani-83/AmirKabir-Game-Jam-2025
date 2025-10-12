using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance;

   private CinemachineVirtualCamera virtualCamera;
    private float shakerTimer;
    private void Awake()
    {
        Instance = this;
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
    }
    public void ShakeCamera(float intensity,float time )
    {
        Debug.Log("Shaaakeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin =virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>(); 
        cinemachineBasicMultiChannelPerlin. m_AmplitudeGain=intensity;
        shakerTimer = time;
    }

    private void Update()
    {
        if (shakerTimer > 0)
        {
            shakerTimer -= Time.deltaTime;
            if (shakerTimer < 0)
            {
                CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0f;
            }
        }
    }
}
