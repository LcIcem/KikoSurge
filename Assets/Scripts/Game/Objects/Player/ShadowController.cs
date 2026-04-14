using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LcIcemFramework.Managers;
using LcIcemFramework.Managers.Pool;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 残影脚本
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ShadowController : MonoBehaviour, IPoolable
{
    private SpriteRenderer _sr;
    [SerializeField] private float _lifeTime = 2f;   // 残影生命周期
    [SerializeField] private float _velocity = 0.4f; // 残影速度
    [SerializeField] private float _acc = 1f; // 残影加速度
    [SerializeField] private List<Color> _randomColors; // 随机颜色列表

    private Queue<Vector3> targetPosQueue = new();
    private Queue<Sprite> spriteQueue = new();
    private Queue<bool> flipQueue = new();
    private Vector3 chasePos;


    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void Init(SpriteRenderer sr)
    {
        StartCoroutine(GenerateShadow(sr));
    }

    public void OnSpawn()
    {
        
    }

    public void OnDespawn()
    {
        StopAllCoroutines();
        targetPosQueue.Clear();
        spriteQueue.Clear();
        flipQueue.Clear();
    }
    
    private IEnumerator GenerateShadow(SpriteRenderer targetSr)
    {
        _sr.sprite = targetSr.sprite;
        _sr.flipX = targetSr.flipX;
        _sr.color = _randomColors[Random.Range(0, _randomColors.Count)];

        chasePos = targetSr.transform.position;
        transform.position = chasePos;

        targetPosQueue.Clear();
        spriteQueue.Clear();
        flipQueue.Clear();

        float velocity = _velocity;
        float alpha = _sr.color.a;
        float alphaVelocity = alpha / _lifeTime;
        float dt = 0.05f;

        spriteQueue.Enqueue(targetSr.sprite);
        flipQueue.Enqueue(targetSr.flipX);


        while (alpha > 0)
        {
            yield return new WaitForSeconds(dt);
            alpha -= dt * alphaVelocity;
            _sr.color = new Color(_sr.color.r, _sr.color.g, _sr.color.b, alpha);

            targetPosQueue.Enqueue(targetSr.transform.position);
            spriteQueue.Enqueue(targetSr.sprite);
            flipQueue.Enqueue(targetSr.flipX);

            // 该次可移动的距离
            float distance = dt * velocity;
            // 累计速度
            velocity += dt * _acc;

            // 该次需要移动的距离
            float distanceMoveTo = (chasePos - transform.position).magnitude;

            // 如果可移动距离小于需要移动距离
            if (distance < distanceMoveTo)
            {
                transform.position = Vector3.MoveTowards(transform.position, chasePos, distance);
            }
            else
            {
                Vector3 curPos = chasePos;
                chasePos = targetPosQueue.Dequeue();
                _sr.sprite = spriteQueue.Dequeue();
                _sr.flipX = spriteQueue.Dequeue();

                distance -= distanceMoveTo;
                transform.position = Vector3.MoveTowards(curPos, chasePos, distance);
            }

            if (Vector3.Distance(transform.position, targetSr.transform.position) < 0.0001f)
            {
                break;
            }
        }
        ManagerHub.Pool.Release(gameObject);
    }
}
