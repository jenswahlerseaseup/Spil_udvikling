using System.Collections;
using UnityEngine;

/// <summary>
/// Shake the tree to advance the apple_harvest quest.
/// Plays a brief rotation wobble animation on interact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ShakeableTree : MonoBehaviour, IInteractable
{
    [SerializeField] private string questId         = "apple_harvest";
    [SerializeField] private int    mischiefOnShake = 0;

    private bool shaken;

    public string InteractionPrompt => shaken ? string.Empty : "Ryst";

    public void Interact(PlayerInteractor interactor)
    {
        if (shaken) return;

        var qm     = QuestManager.Instance;
        var status = qm?.GetStatus(questId) ?? QuestState.NotStarted;

        if (status == QuestState.NotStarted)
        {
            interactor.ShowMessage(string.Empty, "Smukke aebletrae. Tal med gaardejeren foerst.");
            return;
        }

        if (status == QuestState.Completed)
        {
            interactor.ShowMessage(string.Empty, "Du har allerede samlet aeblet.");
            return;
        }

        shaken = true;
        qm.RecordProgress(questId);

        var data = qm.GetData(questId);
        var caught = data?.StepProgress ?? 0;
        interactor.ShowMessage(string.Empty, "Pluds! Et aeble falder ned. (" + caught + " / 3)");

        if (mischiefOnShake > 0)
            MischiefSystem.Instance?.AddMischief(mischiefOnShake, "Rystede aebletrae");

        StartCoroutine(WobbleAnimation());
    }

    private IEnumerator WobbleAnimation()
    {
        var startRot = transform.eulerAngles;
        var elapsed  = 0f;
        const float duration = 0.45f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var angle = Mathf.Sin(elapsed * Mathf.PI * 9f) * 9f * (1f - elapsed / duration);
            transform.rotation = Quaternion.Euler(0f, 0f, startRot.z + angle);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(startRot);
    }
}
