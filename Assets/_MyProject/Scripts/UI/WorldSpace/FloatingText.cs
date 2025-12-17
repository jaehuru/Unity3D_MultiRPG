using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private Vector3 moveDirection = Vector3.up;

    private float _lifeTimer;
    private Color _originalColor;
    
    private FloatingTextPool _pool;

    private void Awake()
    {
        if (damageText != null)
        {
            _originalColor = damageText.color;
        }
    }
    
    private void Start()
    {
        _pool = FloatingTextPool.Instance;
    }

    public void Show(int damage, Vector3 position)
    {
        _lifeTimer = lifeTime;
        transform.position = position;
        if (damageText != null)
        {
            damageText.text = damage.ToString();
            damageText.color = _originalColor;
        }
        
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (_lifeTimer <= 0) return;
        
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        _lifeTimer -= Time.deltaTime;
        
        if (_lifeTimer < lifeTime / 2)
        {
            if (damageText != null)
            {
                float alpha = Mathf.Lerp(0f, 1f, _lifeTimer / (lifeTime / 2));
                damageText.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            }
        }

        if (_lifeTimer <= 0)
        {
            _pool?.Return(this);
        }
    }
}