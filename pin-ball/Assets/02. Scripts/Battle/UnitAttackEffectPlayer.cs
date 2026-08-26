using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitAttackEffectPlayer : MonoBehaviour
{
    [SerializeField] private GameObject arrowEffectPrefab;
    [SerializeField] private string[] arrowUnitIds = Array.Empty<string>();
    [SerializeField] private GameObject fireEffectPrefab;
    [SerializeField] private string[] fireUnitIds = Array.Empty<string>();
    [SerializeField] private GameObject muzzleFlashEffectPrefab;
    [SerializeField] private string[] muzzleFlashUnitIds = Array.Empty<string>();
    [SerializeField] private GameObject targetEffectPrefab;
    [SerializeField] private string[] targetEffectUnitIds = Array.Empty<string>();
    [SerializeField, Min(0.01f)] private float projectileDuration = 0.18f;
    [SerializeField, Min(0f)] private float projectileOriginDistance = 0.12f;
    [SerializeField, Min(0.01f)] private float muzzleFlashDuration = 0.21f;
    [SerializeField, Min(0f)] private float muzzleDistance = 0.28f;
    [SerializeField] private float muzzleHeight = 0.08f;
    [SerializeField, Min(0.01f)] private float targetEffectDuration = 0.75f;

    private readonly Dictionary<GameObject, GameObject> _instances = new();
    private readonly Dictionary<GameObject, Coroutine> _animations = new();

    public void Play(string unitId, UnitBase target)
    {
        if (string.IsNullOrEmpty(unitId) || target == null)
        {
            return;
        }

        if (Contains(arrowUnitIds, unitId) ||
            unitId is "cat1" or "cat3" or "cat5")
        {
            PlayProjectile(arrowEffectPrefab, target.transform.position);
        }

        if (Contains(fireUnitIds, unitId) || unitId.StartsWith("rabbit"))
        {
            PlayProjectile(fireEffectPrefab, target.transform.position);
        }

        if (Contains(muzzleFlashUnitIds, unitId) ||
            unitId is "cat2" or "cat4")
        {
            PlayMuzzleFlash(target.transform.position);
        }

        if (Contains(targetEffectUnitIds, unitId))
        {
            PlayTargetEffect(target.transform.position);
            SoundManager.PlaySFXIfAvailable(SoundName.BossWind);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _animations.Clear();

        foreach (var instance in _instances.Values)
        {
            if (instance != null)
            {
                instance.SetActive(false);
            }
        }
    }

    private void PlayProjectile(GameObject prefab, Vector3 targetPosition)
    {
        var effect = GetOrCreate(prefab);
        if (effect == null)
        {
            return;
        }

        StopAnimation(effect);
        _animations[effect] = StartCoroutine(
            AnimateProjectile(effect, targetPosition));
    }

    private IEnumerator AnimateProjectile(
        GameObject effect,
        Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.right;
        Vector3 startPosition = transform.position +
                                normalizedDirection * projectileOriginDistance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        effect.SetActive(false);
        effect.transform.SetPositionAndRotation(
            startPosition,
            Quaternion.Euler(0f, 0f, angle));
        effect.SetActive(true);

        float elapsed = 0f;
        while (elapsed < projectileDuration)
        {
            float normalizedTime = Mathf.Clamp01(elapsed / projectileDuration);
            effect.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                normalizedTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        effect.transform.position = targetPosition;
        effect.SetActive(false);
        _animations.Remove(effect);
    }

    private void PlayMuzzleFlash(Vector3 targetPosition)
    {
        var effect = GetOrCreate(muzzleFlashEffectPrefab);
        if (effect == null)
        {
            return;
        }

        StopAnimation(effect);

        Vector3 direction = targetPosition - transform.position;
        Vector3 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.left;
        Vector3 muzzlePosition = transform.position +
                                 normalizedDirection * muzzleDistance +
                                 Vector3.up * muzzleHeight;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        effect.SetActive(false);
        effect.transform.SetPositionAndRotation(
            muzzlePosition,
            Quaternion.Euler(0f, 0f, angle));
        effect.SetActive(true);
        effect.GetComponent<Animator>()?.Play(0, 0, 0f);
        _animations[effect] = StartCoroutine(
            HideAfter(effect, muzzleFlashDuration));
    }

    private void PlayTargetEffect(Vector3 targetPosition)
    {
        var effect = GetOrCreate(targetEffectPrefab);
        if (effect == null)
        {
            return;
        }

        StopAnimation(effect);
        effect.SetActive(false);
        effect.transform.SetPositionAndRotation(
            targetPosition,
            Quaternion.identity);
        effect.SetActive(true);
        effect.GetComponent<Animator>()?.Play(0, 0, 0f);
        _animations[effect] = StartCoroutine(
            HideAfter(effect, targetEffectDuration));
    }

    private IEnumerator HideAfter(GameObject effect, float duration)
    {
        yield return new WaitForSeconds(duration);
        effect.SetActive(false);
        _animations.Remove(effect);
    }

    private GameObject GetOrCreate(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        if (_instances.TryGetValue(prefab, out var instance) && instance != null)
        {
            return instance;
        }

        instance = Instantiate(prefab, transform);
        instance.name = prefab.name;
        instance.SetActive(false);
        _instances[prefab] = instance;
        return instance;
    }

    private void StopAnimation(GameObject effect)
    {
        if (!_animations.TryGetValue(effect, out var animation) ||
            animation == null)
        {
            return;
        }

        StopCoroutine(animation);
        _animations.Remove(effect);
    }

    private static bool Contains(string[] unitIds, string unitId)
    {
        if (unitIds == null)
        {
            return false;
        }

        foreach (string candidate in unitIds)
        {
            if (candidate == unitId)
            {
                return true;
            }
        }

        return false;
    }
}
