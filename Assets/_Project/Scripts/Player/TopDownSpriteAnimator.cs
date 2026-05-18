using UnityEngine;

// Superseded by DirectionalSpriteAnimator. Remove this component from the player
// and add DirectionalSpriteAnimator instead.
[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class TopDownSpriteAnimator : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning("TopDownSpriteAnimator is deprecated. Replace it with DirectionalSpriteAnimator on " + gameObject.name, this);
        enabled = false;
    }
}
