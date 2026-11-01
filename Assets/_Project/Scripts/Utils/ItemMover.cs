using System;
using System.Collections.Generic;
using UnityEngine;

namespace EcoGame.Utils
{
    /// <summary>Moves a visual item along points, or toward a moving target, without physics.</summary>
    public class ItemMover : MonoBehaviour
    {
        private const float ARRIVE_DISTANCE = 0.05f;
        private const float FOLLOW_ARRIVE_DISTANCE = 0.4f;
        private const float FOLLOW_MIN_SCALE = 0.3f;

        private readonly List<Vector3> _path = new();
        private Transform _follow;
        private Vector3 _followOffset;
        private Vector3 _startScale;
        private float _startDistance;
        private float _speed;
        private bool _destroyAtEnd;
        private Action _onArrive;
        private int _index;

        /// <summary>Moves through the points in order.</summary>
        public static ItemMover Move(GameObject item, float speed, bool destroyAtEnd, Action onArrive, params Vector3[] path)
        {
            var mover = Attach(item, speed, destroyAtEnd, onArrive);
            mover._path.AddRange(path);
            return mover;
        }

        /// <summary>Flies to a moving target, shrinking on the way, then destroys the item.</summary>
        public static ItemMover FlyTo(GameObject item, Transform target, float speed)
        {
            var mover = Attach(item, speed, true, null);
            mover._follow = target;
            mover._followOffset = Vector3.up;
            mover._startDistance = Mathf.Max(Vector3.Distance(item.transform.position, target.position + Vector3.up), 0.01f);
            return mover;
        }

        private static ItemMover Attach(GameObject item, float speed, bool destroyAtEnd, Action onArrive)
        {
            var mover = item.GetComponent<ItemMover>();
            if (mover == null) mover = item.AddComponent<ItemMover>();
            mover._path.Clear();
            mover._follow = null;
            mover._index = 0;
            mover._speed = speed;
            mover._destroyAtEnd = destroyAtEnd;
            mover._onArrive = onArrive;
            mover._startScale = item.transform.localScale;
            mover.enabled = true;
            return mover;
        }

        private void Update()
        {
            if (_index < _path.Count)
            {
                transform.position = Vector3.MoveTowards(transform.position, _path[_index], _speed * Time.deltaTime);
                if ((transform.position - _path[_index]).sqrMagnitude <= ARRIVE_DISTANCE * ARRIVE_DISTANCE) _index++;
                if (_index < _path.Count || _follow != null) return;
                Finish();
                return;
            }

            if (_follow == null)
            {
                Finish();
                return;
            }

            Vector3 target = _follow.position + _followOffset;
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);
            float distance = Vector3.Distance(transform.position, target);
            transform.localScale = _startScale * Mathf.Lerp(FOLLOW_MIN_SCALE, 1f, distance / _startDistance);
            if (distance <= FOLLOW_ARRIVE_DISTANCE) Finish();
        }

        private void Finish()
        {
            _onArrive?.Invoke();
            if (_destroyAtEnd) Destroy(gameObject);
            else enabled = false;
        }
    }
}
