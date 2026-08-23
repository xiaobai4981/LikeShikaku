using UnityEngine;
using Cysharp.Threading.Tasks;



namespace Core
{
    public class GameModeBase : MonoBehaviour
    {
        private World m_world;
        public World World => m_world;



        public void PreInit(World world)
        {
            m_world = world;
        }



        public virtual UniTask Init()
        {
            return default;
        }



        public virtual void Cleanup()
        {

        }



        public virtual void Tick()
        {

        }



        public virtual void FixedTick()
        {

        }



        public virtual void LateTick()
        {

        }
    }
}
