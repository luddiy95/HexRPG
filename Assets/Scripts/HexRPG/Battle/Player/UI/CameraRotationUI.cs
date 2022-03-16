using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;

namespace HexRPG.Battle.Player.UI
{
    using Battle.UI;

    public class CameraRotationUI : MonoBehaviour, ICharacterUI
    {
        [SerializeField] Transform _cameraRotLeft;
        [SerializeField] Transform _cameraRotRight;

        void ICharacterUI.Bind(ICharacterComponentCollection character)
        {
            if(character is IPlayerComponentCollection playerOwner)
            {
                //TODO: playerOwner��IDLE�̂Ƃ��̂�enable��
                //TODO: ����ȏ��]�ł��Ȃ��Ƃ���disable��
            }
        }

        IObservable<Unit> ICharacterUI.OnBack => null;
    }
}
