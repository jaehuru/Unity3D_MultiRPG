using System;
// Unity
using UnityEngine;


public class VisibilityController : MonoBehaviour
{
    public event Action<bool> OnVisibilityChanged;

    private void OnBecameVisible()
    {
        OnVisibilityChanged?.Invoke(true);
    }
    
    private void OnBecameInvisible()
    {
        OnVisibilityChanged?.Invoke(false);
    }
}
