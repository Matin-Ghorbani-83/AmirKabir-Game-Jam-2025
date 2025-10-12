using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    [SerializeField] float reviveTime;
    [SerializeField] GameObject[] hearts;
    [SerializeField] ParticleSystem particle;
    [SerializeField] GameObject loseUI;
    bool firstHealth = true;
    bool nullHealth = false;
    float alpha = 0f;
    float progressUI;
    private void Start()
    {
        PlayerHealthSystem.instance.OnPlayerDied += die;
        PlayerHealthSystem.instance.OnHeartRegenProgress += Instance_OnHeartRegenProgress;
    }

    private void Instance_OnHeartRegenProgress(int arg1, float Progress)
    {
        progressUI = Progress;
        Revive();
        Debug.Log(progressUI);
    }

    private void die(Vector3 obj)
    {
        loseUI.SetActive(true);
    }

    public void lose()
    {
        SceneManager.LoadScene(0);
    }

    private void Update()
    {
        //revive();
    }

    public void Damage()
    {
        if (firstHealth)
        {
            hearts[1].SetActive(false);
            firstHealth = false;
        }
        else
        {
            hearts[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            nullHealth = true;
        }
    }

    void Revive()
    {
        if (nullHealth)
        {
            StartCoroutine(reviving(progressUI));
            nullHealth = false;
        }
    }

    IEnumerator reviving(float progres)
    {
        int heartFillDuration = 7;
        int steps = Mathf.Max(1, 3);
        float stepDur = Mathf.Max(0.01f, heartFillDuration / steps);
        //alpha += (1f / reviveTime);
        alpha += progres;
        hearts[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);
        Debug.Log("Alph: " + alpha);
        yield return new WaitForSeconds(stepDur);
        if (alpha < 1f)
            StartCoroutine(reviving(progres));
        else if (alpha >= 1f)
        {
            var emissin = particle.emission;
            emissin.enabled = true;

            particle.Play();

            alpha = 0f;
        }
    }
}
