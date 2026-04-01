using UnityEngine;
using NeoSaveGames;
using NeoSaveGames.Serialization;

namespace NeoFPS.Samples.SinglePlayer.MultiScene
{
    public class BallSpawner : MonoBehaviour, INeoSerializableComponent
    {
        public Rigidbody ballPrefab = null;
        public Transform spawnPoint = null;
        public float interval = 2f;
        public int maxBalls = 25;

        static readonly NeoSerializationKey k_TimerKey = new NeoSerializationKey("timer");

        const float k_BallSize = 0.8f;

        private NeoSerializedGameObject m_NSGO = null;
        private float m_Timer = 0.5f;

        public static int activeBallCount
        {
            get;
            set;
        }

        private void Awake()
        {
            m_NSGO = GetComponent<NeoSerializedGameObject>();
        }

        private void Update()
        {
            m_Timer -= Time.deltaTime;
            if (m_Timer < 0f && activeBallCount < maxBalls)
            {
                m_Timer = interval;
                SpawnBall();
            }
        }

        public void SpawnBall()
        {
            var ball = NeoPrefabFactory.InstantiatePrefab(ballPrefab, this, transform, spawnPoint.position, Quaternion.identity, TranslationSpace.World);
            ball.transform.localScale = Vector3.one * k_BallSize;
            ball.angularVelocity = Random.insideUnitSphere * 50f;
        }

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_TimerKey, m_Timer);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_TimerKey, out m_Timer, m_Timer);
        }
    }
}