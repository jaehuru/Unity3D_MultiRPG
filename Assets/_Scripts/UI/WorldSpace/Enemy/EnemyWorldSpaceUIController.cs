// Unity
using UnityEngine;
using Unity.Netcode;
// Project
using System.Collections;

public class EnemyWorldSpaceUIController : WorldSpaceUIController
{
    private bool _isCombatVisible = false;
    private Coroutine _combatUICoroutine;

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

        yield return new WaitForSeconds(3f);

        _isCombatVisible = false;
        UpdateUIVisibility();
        _combatUICoroutine = null;
    }
}