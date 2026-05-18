using UnityEngine;

[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class TopDownSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Transform visualsRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color idleColor = new(0.36f, 0.88f, 0.65f);
    [SerializeField] private Color walkColor = new(0.56f, 0.95f, 0.74f);
    [SerializeField, Min(0f)] private float bobAmplitude = 0.045f;
    [SerializeField, Min(0f)] private float bobSpeed = 11f;

    private TopDownPlayerMotor motor;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        motor = GetComponent<TopDownPlayerMotor>();

        if (visualsRoot == null)
        {
            visualsRoot = transform.Find("Visuals");
        }

        if (spriteRenderer == null && visualsRoot != null)
        {
            spriteRenderer = visualsRoot.GetComponent<SpriteRenderer>();
        }

        if (visualsRoot != null)
        {
            baseLocalPosition = visualsRoot.localPosition;
        }
    }

    private void Update()
    {
        if (visualsRoot == null || spriteRenderer == null)
        {
            return;
        }

        var isMoving = motor.IsMoving;
        var bob = isMoving ? Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobAmplitude : 0f;
        visualsRoot.localPosition = baseLocalPosition + Vector3.up * bob;

        var facing = motor.FacingDirection;
        if (Mathf.Abs(facing.x) > 0.1f)
        {
            visualsRoot.localScale = new Vector3(Mathf.Sign(facing.x), 1f, 1f);
        }

        spriteRenderer.color = Color.Lerp(spriteRenderer.color, isMoving ? walkColor : idleColor, Time.deltaTime * 12f);
    }
}
