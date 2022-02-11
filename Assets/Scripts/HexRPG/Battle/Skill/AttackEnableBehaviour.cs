using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    public class AttackEnableBehaviour : PlayableBehaviour
    {
        //PlayableAssetで設定したCubeを設定したプロパティ
        public IAttackSkillController attackSkillController;

        //タイムラインが開始したときに実行される
        public override void OnGraphStart(Playable playable)
        {

        }

        //タイムラインが停止したときに実行される
        public override void OnGraphStop(Playable playable)
        {

        }


        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {

        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {

        }


        public override void PrepareFrame(Playable playable, FrameData info)
        {

        }
    }
}
