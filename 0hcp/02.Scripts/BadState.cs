using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace hcp
{
    public interface IBadState { }
    public interface ICanNotMove { }
    public interface ICanNotActive { }

    public abstract class BadState<T> : IBadState where T : class, new()
    {
        protected E_BadState state;
        public E_BadState State
        {
            get
            {
                return state;
            }
        }
        public static T instance = new T();
    }

    public class NoneBadState : BadState<NoneBadState>
    {
        public NoneBadState()
        {
            state = E_BadState.None;
        }
    }

    public class BadStateStun : BadState<BadStateStun>, ICanNotMove, ICanNotActive
    {
        public BadStateStun()
        {
            state = E_BadState.Stun;
        }
    }
}