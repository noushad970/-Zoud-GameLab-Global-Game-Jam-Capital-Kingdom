using System.Collections;
using UnityEngine;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Controls train movement along a railway track between two endpoints.
    /// Traverses from start to end, waits a random 10-20 seconds at the station,
    /// then returns to the start station without rotating (push-pull reverse drive).
    /// </summary>
    public class TrainPathFollower : MonoBehaviour
    {
        [Header("Route Endpoints")]
        [SerializeField] private Vector3 startPoint;
        [SerializeField] private Vector3 endPoint;
        [SerializeField] private bool autoDetectEndpointsFromCurrentPos = true;
        [SerializeField] private float southStationZ = -200.46f;
        [SerializeField] private float northStationZ = 32.74f;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 16f;
        [SerializeField] private float arrivalDistance = 0.4f;

        [Header("Station Wait Times")]
        [SerializeField] private float minWaitSeconds = 10f;
        [SerializeField] private float maxWaitSeconds = 20f;

        [Header("Orientation")]
        [Tooltip("If false, train retains its initial orientation and does not rotate when traveling in reverse.")]
        [SerializeField] private bool rotateOnReverse = false;
        [SerializeField] private float turnAroundSpeed = 120f; // degrees per sec

        private enum TrainState
        {
            MovingToEnd,
            WaitingAtEnd,
            MovingToStart,
            WaitingAtStart
        }

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private TrainState currentState = TrainState.MovingToEnd;
        [SerializeField] private float currentWaitTimer = 0f;

        private Quaternion initialRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureSceneTrains()
        {
            string[] trainNames = new string[] { "Train", "Train (1)", "Train (2)", "Train (3)" };
            foreach (string tName in trainNames)
            {
                GameObject trainObj = GameObject.Find(tName);
                if (trainObj != null && trainObj.GetComponent<TrainPathFollower>() == null)
                {
                    trainObj.AddComponent<TrainPathFollower>();
                }
            }

            GameObject buildingsObj = GameObject.Find("Buildings");
            if (buildingsObj != null)
            {
                foreach (Transform child in buildingsObj.transform)
                {
                    if (child.name.StartsWith("Train") && !child.name.Contains("Carriage"))
                    {
                        if (child.GetComponent<TrainPathFollower>() == null)
                        {
                            child.gameObject.AddComponent<TrainPathFollower>();
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            if (autoDetectEndpointsFromCurrentPos)
            {
                ConfigureEndpoints();
            }

            // Start moving towards whichever destination is ahead
            float distToEnd = Vector3.Distance(transform.position, endPoint);
            float distToStart = Vector3.Distance(transform.position, startPoint);

            if (distToEnd > distToStart)
            {
                currentState = TrainState.MovingToEnd;
            }
            else
            {
                currentState = TrainState.MovingToStart;
            }

            StartCoroutine(TrainRoutine());
        }

        private void ConfigureEndpoints()
        {
            Vector3 pos = transform.position;

            // In this city layout, the railway tracks run along Z:
            // South end is around Z = -191.46, North end is around Z = +32.74
            float midZ = (southStationZ + northStationZ) * 0.5f;

            if (pos.z < midZ)
            {
                // Initialized near South -> start at south, end at north
                startPoint = new Vector3(pos.x, pos.y, southStationZ);
                endPoint = new Vector3(pos.x, pos.y, northStationZ);
            }
            else
            {
                // Initialized near North -> start at north, end at south
                startPoint = new Vector3(pos.x, pos.y, northStationZ);
                endPoint = new Vector3(pos.x, pos.y, southStationZ);
            }
        }

        public void SetRoute(Vector3 start, Vector3 end, float speed = 16f)
        {
            startPoint = start;
            endPoint = end;
            moveSpeed = speed;
            autoDetectEndpointsFromCurrentPos = false;
        }

        private IEnumerator TrainRoutine()
        {
            while (true)
            {
                // 1. Move to End Point
                currentState = TrainState.MovingToEnd;
                yield return StartCoroutine(MoveToTarget(endPoint));

                // 2. Wait at End Station for random 10 to 20 seconds
                currentState = TrainState.WaitingAtEnd;
                float waitAtEnd = Random.Range(minWaitSeconds, maxWaitSeconds);
                currentWaitTimer = waitAtEnd;
                while (currentWaitTimer > 0f)
                {
                    currentWaitTimer -= Time.deltaTime;
                    yield return null;
                }

                // Turn around only if rotateOnReverse is explicitly enabled
                if (rotateOnReverse)
                {
                    yield return StartCoroutine(RotateTowards(startPoint));
                }

                // 3. Move back to Start Point (pushes backward without rotating)
                currentState = TrainState.MovingToStart;
                yield return StartCoroutine(MoveToTarget(startPoint));

                // 4. Wait at Start Station for random 10 to 20 seconds
                currentState = TrainState.WaitingAtStart;
                float waitAtStart = Random.Range(minWaitSeconds, maxWaitSeconds);
                currentWaitTimer = waitAtStart;
                while (currentWaitTimer > 0f)
                {
                    currentWaitTimer -= Time.deltaTime;
                    yield return null;
                }

                // Turn around only if rotateOnReverse is explicitly enabled
                if (rotateOnReverse)
                {
                    yield return StartCoroutine(RotateTowards(endPoint));
                }
            }
        }

        private IEnumerator MoveToTarget(Vector3 target)
        {
            if (rotateOnReverse)
            {
                Vector3 direction = (target - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }
            else
            {
                // Maintain fixed orientation without any rotation
                transform.rotation = initialRotation;
            }

            while (Vector3.Distance(transform.position, target) > arrivalDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                if (!rotateOnReverse)
                {
                    transform.rotation = initialRotation;
                }
                yield return null;
            }

            transform.position = target;
            if (!rotateOnReverse)
            {
                transform.rotation = initialRotation;
            }
        }

        private IEnumerator RotateTowards(Vector3 nextDestination)
        {
            Vector3 direction = (nextDestination - transform.position).normalized;
            if (direction == Vector3.zero) yield break;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnAroundSpeed * Time.deltaTime);
                yield return null;
            }

            transform.rotation = targetRotation;
        }
    }
}
