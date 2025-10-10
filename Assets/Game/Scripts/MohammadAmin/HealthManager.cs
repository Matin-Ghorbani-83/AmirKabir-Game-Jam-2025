using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    [SerializeField] float reviveTime;
    [SerializeField] GameObject[] hearts;
    [SerializeField] ParticleSystem particle;
    bool firstHealth = true;
    bool nullHealth = false;
    float alpha = 0f;

    private void Start()
    {
        PlayerHealthSystem.instance.OnPlayerDied += die;
    }

    private void die(Vector3 obj)
    {
        SceneManager.LoadScene(0);
    }

    private void Update()
    {
        revive();
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

    void revive()
    {
        if (nullHealth)
        {
            StartCoroutine(reviving());
            nullHealth = false;
        }
    }

    IEnumerator reviving()
    {
        alpha += (1f / reviveTime);
        hearts[0].gameObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, alpha);
        Debug.Log("Alph: " + alpha);
        yield return new WaitForSeconds(1);
        if (alpha < 1f)
            StartCoroutine(reviving());
        else if (alpha >= 1f)
        {
            var emissin = particle.emission;
            emissin.enabled = true;

            particle.Play();

            alpha = 0f;
        }
    }
}
