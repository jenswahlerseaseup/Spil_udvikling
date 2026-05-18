using UnityEngine;

// Drives a SpriteRenderer with 4-directional walk/idle sprites at a pixel-art frame rate.
// The movement system owns direction; this component only translates that state into visuals.
[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class DirectionalSpriteAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private CharacterSpriteSet spriteSet;
    [SerializeField, Min(1)] private int fps = 8;

    private TopDownPlayerMotor motor;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        motor = GetComponent<TopDownPlayerMotor>();

        if (spriteRenderer == null)
        {
            var visuals = transform.Find("Visuals");
            spriteRenderer = visuals != null
                ? visuals.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteSet == null)
        {
            spriteSet = PixelArtCharacterSprites.CreateDefault();
        }
    }

    private void Update()
    {
        if (spriteRenderer == null || spriteSet == null)
        {
            return;
        }

        var facing = motor.FacingDirection;
        var isMoving = motor.IsMoving;
        var mirrorLeftFallback = false;

        Sprite idleSprite;
        Sprite[] walkFrames;

        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.y))
        {
            if (facing.x >= 0f)
            {
                idleSprite = spriteSet.sideRightIdle;
                walkFrames = spriteSet.sideRightWalk;
            }
            else
            {
                idleSprite = spriteSet.sideLeftIdle;
                walkFrames = spriteSet.sideLeftWalk;
                mirrorLeftFallback = spriteSet.sideLeftIdle == spriteSet.sideRightIdle;
            }
        }
        else if (facing.y > 0f)
        {
            idleSprite = spriteSet.backIdle;
            walkFrames = spriteSet.backWalk;
        }
        else
        {
            idleSprite = spriteSet.frontIdle;
            walkFrames = spriteSet.frontWalk;
        }

        spriteRenderer.flipX = mirrorLeftFallback;

        if (!isMoving || walkFrames == null || walkFrames.Length == 0)
        {
            spriteRenderer.sprite = idleSprite;
            frameTimer = 0f;
            frameIndex = 0;
            return;
        }

        frameTimer += Time.deltaTime;
        var frameDuration = 1f / fps;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % walkFrames.Length;
        }

        spriteRenderer.sprite = walkFrames[frameIndex];
    }
}
