using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Cheats.AIBot
{
    public class AIState
    {
        protected enum InternalState { Failed, IsReady, Success }

        protected virtual InternalState GetCondition() => InternalState.IsReady;
        protected virtual InternalState OnUpdate() => InternalState.Success;

        List<AIState> _handlers = new List<AIState>();
        
        public void Register( AIState state )
        {
            if ( state == null )
                return;

            _handlers.Add( state );
        }

        public bool Invoke()
        {
            InternalState state = GetCondition();
            switch( state )
            {
                case InternalState.Failed:
                return false;

                case InternalState.IsReady:
                for ( int i = 0; i < _handlers.Count; i++ )
                {
                    if ( _handlers[i] == null )
                    {
                        _handlers.RemoveAt( i );
                        i = i - 1;
                        continue;
                    }
                    if ( _handlers[i].Invoke() )
                        continue;
                    return false; // can't fully execute - not ready yet
                }
                return ( state = OnUpdate() ) == InternalState.Success;

                case InternalState.Success:
                return true;
            }

            throw new NotImplementedException();
        }
    }
}