using UnityEngine;
using UnityEngine.InputSystem;

namespace Campaign.Game.Controller
{
    /// <summary>
    /// 게임 흐름이 특정 입력 장치나 Input System API를 직접 알지 않게 하는 경계입니다.
    /// 테스트에서는 고정 벡터를 반환하는 대역 구현을 주입할 수 있습니다.
    /// </summary>
    public interface IPlayerInputSource
    {
        Vector2 ReadMovement();
    }

    /// <summary>현재 프로토타입의 키보드 입력을 새 Input System으로 읽는 어댑터입니다.</summary>
    public sealed class UnityInputSystemSource : IPlayerInputSource
    {
        public Vector2 ReadMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;
            var direction = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x += 1f;
            // 대각선 입력의 크기를 1로 제한해 이동 속도를 일정하게 유지합니다.
            return direction.normalized;
        }
    }
}
