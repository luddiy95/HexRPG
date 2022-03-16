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
                //TODO: playerOwner‚ªIDLE‚Ì‚Æ‚«‚Ì‚İenable‚É
                //TODO: ‚»‚êˆÈã‰ñ“]‚Å‚«‚È‚¢‚Æ‚«‚Ídisable‚É
            }
        }

        IObservable<Unit> ICharacterUI.OnBack => null;
    }
}
