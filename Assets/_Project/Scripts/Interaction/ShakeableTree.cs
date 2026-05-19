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
    [SerializeField] private ItemDefinition appleItem;
    [SerializeField] private Sprite pickupSprite;
    [SerializeField] private Vector2 dropOffset = new(0.15f, -0.55f);

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
        DropApplePickup();

        var data = qm.GetData(questId);
        var caught = data?.StepProgress ?? 0;
        var progressText = qm.IsReadyToComplete(questId)
            ? "Vend tilbage til gaardejeren."
            : "(" + caught + " / 3)";
        interactor.ShowMessage(string.Empty, "Pluds! Et aeble falder ned. " + progressText);

        if (mischiefOnShake > 0)
            MischiefSystem.Instance?.AddMischief(mischiefOnShake, "Rystede aebletrae");

        StartCoroutine(WobbleAnimation());
    }

    private void DropApplePickup()
    {
        if (appleItem == null)
        {
            return;
        }

        var pickup = new GameObject("Fallen Apple");
        pickup.transform.position = transform.position + (Vector3)dropOffset;

        var renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = pickupSprite != null ? pickupSprite : appleItem.Icon;
        renderer.sortingOrder = 20;

        var sorter = pickup.AddComponent<AutoYSort2D>();
        sorter.Configure(5000, 100, 0f, true);

        var collider = pickup.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.25f;

        pickup.AddComponent<PickupItem>().Configure(appleItem, 1, 0);
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
