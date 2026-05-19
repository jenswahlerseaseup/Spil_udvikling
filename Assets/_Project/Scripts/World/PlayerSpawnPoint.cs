using UnityEngine;

/// <summary>
/// Marks the default spawn position and respawns the player on death.
/// The first enabled instance in the scene is used; place it at the farmyard hub.
/// </summary>
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    public static PlayerSpawnPoint Default { get; private set; }

    [SerializeField] private bool respawnOnDeath = true;

    private void OnEnable()
    {
        if (Default == null) Default = this;
    }

    private void OnDisable()
    {
        if (Default == this) Default = null;
    }

    private void Start()
    {
        if (!respawnOnDeath) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var health = player.GetComponent<HealthSystem>();
        if (health != null)
            health.Died += () => Respawn(player);
    }

    private void Respawn(GameObject player)
    {
        player.transform.position = transform.position;

        var health = player.GetComponent<HealthSystem>();
        health?.LoadState(health.MaxHealth);

        GameHud.Instance?.ShowNotification("Emil staar op igen...");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.9f, 0.5f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.5f, 1f, 0f));
    }
#endif
}
