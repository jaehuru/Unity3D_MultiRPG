// Unity
using UnityEngine;
using Unity.Netcode;
// Project
using System.Collections;

public class EnemyUIController : WorldSpaceUIController
{
    private bool _isCombatVisible = false;
    private Coroutine _combatUICoroutine;

    // --- 성능 최적화: WaitForSeconds 캐싱 ---
    private WaitForSeconds _threeSecondsWait;

    void Awake()
    {
        _threeSecondsWait = new WaitForSeconds(3f);
    }

    protected override bool ShouldLogicVisible()
    {
        return _isCombatVisible;
    }

    [ClientRpc]
    public void ShowCombatUIForAttackerClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (_combatUICoroutine != null)
        {
            StopCoroutine(_combatUICoroutine);
        }
        _combatUICoroutine = StartCoroutine(CombatUITimer());
    }

    private IEnumerator CombatUITimer()
    {
        _isCombatVisible = true;
        UpdateUIVisibility();

        yield return _threeSecondsWait;

        _isCombatVisible = false;
        UpdateUIVisibility();
        _combatUICoroutine = null;
    }
}