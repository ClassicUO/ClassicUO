using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Cheats.AIBot
{
    internal sealed class MoveHandler : AIState
    {
        AIBot _bot;
        Direction _nextDirection;

        public MoveHandler( AIBot bot )
        {
            _bot = bot;
        }

        protected override InternalState OnUpdate()
        {
            if ( World.Player.Walk( _nextDirection, true ) )
                GameActions.Print( "AI: You have moved a step closer to the target." );

            return InternalState.IsReady;
        }

        protected override InternalState GetCondition()
        {
            if ( _bot.Target == null )
                return InternalState.Failed;

            Mobile follow = _bot.Target;

            int distance = follow.Distance;
            if ( distance > World.ClientViewRange )
            {
                // Start wandering?
                return InternalState.Failed;
            }

            if ( Pathfinder.WalkTo( follow.X, follow.Y, follow.Z, distance) ) {
                return InternalState.Success;
            } else {

                if ( World.Player.Steps.Count > 0 )
                    World.Player.ClearSteps();

                // Check for obstacles (create an obstaclehandler)
                _nextDirection = DirectionHelper.DirectionFromPoints( World.Player.RealScreenPosition, follow.RealScreenPosition );
            }
            return InternalState.IsReady;
        }
    }
}