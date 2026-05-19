using UnityEngine;

public sealed class SoapboxRunController : Singleton<SoapboxRunController>
{
    [SerializeField] private Sprite carSprite;
    [SerializeField] private Vector2 runStart = new(-60f, -31f);
    [SerializeField] private Bounds runCameraBounds = new(new Vector3(-30f, -30f, 0f), new Vector3(90f, 24f, 0f));
    [SerializeField, Min(1f)] private float minimumRunTime = 3f;
    [SerializeField, Min(0f)] private float stopSpeed = 0.45f;

    private PlayerInteractor activeInteractor;
    private PlayerInputReader inputReader;
    private GameObject player;
    private Rigidbody2D playerBody;
    private TopDownPlayerMotor playerMotor;
    private PlayerInteractor playerInteractor;
    private SpriteRenderer[] playerRenderers;
    private Vector3 playerReturnPosition;
    private GameObject car;
    private Rigidbody2D carBody;
    private SoapboxStats currentStats;
    private float startX;
    private float runStartTime;
    private bool running;

    public bool IsRunning => running;

    public void StartRun(PlayerInteractor interactor)
    {
        if (running || interactor == null)
        {
            return;
        }

        activeInteractor = interactor;
        player = interactor.gameObject;
        inputReader = player.GetComponent<PlayerInputReader>();
        playerBody = player.GetComponent<Rigidbody2D>();
        playerMotor = player.GetComponent<TopDownPlayerMotor>();
        playerInteractor = player.GetComponent<PlayerInteractor>();
        playerRenderers = player.GetComponentsInChildren<SpriteRenderer>();
        playerReturnPosition = player.transform.position;

        currentStats = SoapboxProgress.Instance != null
            ? SoapboxProgress.Instance.GetStats(interactor.Inventory)
            : new SoapboxStats { acceleration = 9f, topSpeed = 8f, stability = 1f, weight = 1f };

        SetPlayerExplorationEnabled(false);
        SpawnCar();
        FocusCameraOn(car.transform);

        running = true;
        runStartTime = Time.time;
        startX = car.transform.position.x;
        GameHud.Instance?.ShowNotification("Hold hoejre for fart. Hop over bump med mellemrum.", 3f);
    }

    private void FixedUpdate()
    {
        if (!running || carBody == null || inputReader == null)
        {
            return;
        }

        var input = inputReader.MoveInput;
        var push = Mathf.Clamp01(input.x);
        var targetSpeed = currentStats.topSpeed;
        if (carBody.linearVelocity.x < targetSpeed)
        {
            carBody.AddForce(Vector2.right * (currentStats.acceleration * push / currentStats.weight), ForceMode2D.Force);
        }

        var stabilizer = -carBody.angularVelocity * currentStats.stability * 0.02f;
        carBody.AddTorque(stabilizer, ForceMode2D.Force);

        var elapsed = Time.time - runStartTime;
        if (elapsed > minimumRunTime && carBody.linearVelocity.magnitude < stopSpeed)
        {
            EndRun();
        }
    }

    private void Update()
    {
        if (!running || car == null)
        {
            return;
        }

        var distance = Mathf.Max(0f, car.transform.position.x - startX);
        if (distance > 0f)
        {
            GameHud.Instance?.ShowInteractionPrompt("Distance: " + Mathf.RoundToInt(distance) + " m");
        }
    }

    private void EndRun()
    {
        var distance = Mathf.Max(0f, car.transform.position.x - startX);
        SoapboxProgress.Instance?.RegisterRun(distance);
        WorldFeedbackText.Spawn(car.transform.position + Vector3.up, Mathf.RoundToInt(distance) + " m", new Color(1f, 0.9f, 0.45f));
        GameHud.Instance?.ShowNotification("Run faerdig: " + Mathf.RoundToInt(distance) + " m. Bedste: " +
            Mathf.RoundToInt(SoapboxProgress.Instance != null ? SoapboxProgress.Instance.BestDistance : distance) + " m", 3.5f);

        Destroy(car);
        car = null;
        carBody = null;
        running = false;
        GameHud.Instance?.ShowInteractionPrompt(null);

        if (player != null)
        {
            player.transform.position = playerReturnPosition;
            if (playerBody != null)
            {
                playerBody.position = playerReturnPosition;
            }
        }

        SetPlayerExplorationEnabled(true);
        if (player != null)
        {
            FocusCameraOn(player.transform);
        }
    }

    private void SpawnCar()
    {
        car = new GameObject("Soapbox Car");
        car.transform.position = new Vector3(runStart.x, runStart.y, 0f);

        var renderer = car.AddComponent<SpriteRenderer>();
        renderer.sprite = carSprite;
        renderer.sortingOrder = 30;

        carBody = car.AddComponent<Rigidbody2D>();
        carBody.gravityScale = 2.6f;
        carBody.mass = currentStats.weight;
        carBody.angularDamping = 0.9f;
        carBody.linearDamping = 0.05f;
        carBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = car.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.35f, 0.55f);
        collider.offset = new Vector2(0f, -0.05f);
    }

    private void SetPlayerExplorationEnabled(bool enabled)
    {
        if (playerMotor != null) playerMotor.enabled = enabled;
        if (playerInteractor != null) playerInteractor.enabled = enabled;
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = enabled;
        }

        if (playerRenderers == null)
        {
            return;
        }

        foreach (var renderer in playerRenderers)
        {
            if (renderer != null) renderer.enabled = enabled;
        }
    }

    private void FocusCameraOn(Transform target)
    {
        var cameraObject = Camera.main != null ? Camera.main.gameObject : null;
        if (cameraObject == null)
        {
            return;
        }

        var camera = cameraObject.GetComponent<Camera>();
        if (camera != null)
        {
            camera.orthographicSize = target == player?.transform ? 6f : 5f;
        }

        var follow = cameraObject.GetComponent<FollowCamera2D>();
        if (follow == null)
        {
            return;
        }

        follow.SetTarget(target);
        follow.SetWorldBounds(target == player?.transform
            ? new Bounds(Vector3.zero, new Vector3(48f, 32f, 0f))
            : runCameraBounds);
    }
}
