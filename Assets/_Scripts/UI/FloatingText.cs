using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Vector3 moveDirection = Vector3.up;

    private float lifeTimer;

    void Awake()
    {
        lifeTimer = lifeTime;
    }

    public void SetDamage(int damage)
    {
        if (damageText != null)
        {
            damageText.text = damage.ToString();
        }
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTimer -= Time.deltaTime;
        
        if (lifeTimer < lifeTime / 2)
        {
            if (damageText != null)
            {
                float alpha = Mathf.Lerp(0f, 1f, lifeTimer / (lifeTime / 2));
                damageText.color = new Color(damageText.color.r, damageText.color.g, damageText.color.b, alpha);
            }
        }

        if (lifeTimer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
