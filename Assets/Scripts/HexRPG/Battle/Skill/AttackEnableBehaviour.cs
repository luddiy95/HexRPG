using UnityEngine.Playables;

namespace HexRPG.Battle.Skill
{
    public class AttackEnableBehaviour : PlayableBehaviour
    {
        //PlayableAsset�Őݒ肵��Cube��ݒ肵���v���p�e�B
        public IAttackSkillController attackSkillController;

        //�^�C�����C�����J�n�����Ƃ��Ɏ��s�����
        public override void OnGraphStart(Playable playable)
        {

        }

        //�^�C�����C������~�����Ƃ��Ɏ��s�����
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
