﻿using Assets.Scripts.UserInput;
using UnityEngine;

namespace Assets.Scripts.Player
{
	#region Player State Machine

	/// <summary>
	/// Represents base abstract player state
	/// </summary>
	internal abstract class PlayerState
	{
		#region Properties

		/// <summary>
		/// Reference to the <see cref="PlayerController"/> owner object
		/// </summary>
		internal PlayerController Player { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs base player state
		/// </summary>
		/// <param name="player">Reference to the <see cref="PlayerController"/> owner object</param>
		protected PlayerState( PlayerController player )
		{
			Player = player;
		}

		#endregion

		#region Functions

		/// <summary>
		/// Handles the updating of currently active state
		/// </summary>
		internal abstract void OnUpdate();

		/// <summary>
		/// Defines behavior when player sprite collides with something
		/// </summary>
		/// <param name="collision">Object containing collision details</param>
		internal abstract void OnCollision( Collision2D collision );

		#endregion
	}

	/// <summary>
	/// Represents player state, when player is not doing anything
	/// </summary>
	internal class IdleState : PlayerState
	{
		#region Constructors

		/// <summary>
		/// Constructs base idle state
		/// </summary>
		/// <param name="player">Reference to the <see cref="PlayerController"/> owner object</param>
		internal IdleState( PlayerController player ) : base( player )
		{
			Player.Animator.ResetTrigger( Player.MOVE_ANIMATION_TRIGGER );
		}

		#endregion

		#region Overrides

		internal override void OnCollision( Collision2D collision )
		{
			// intentionally left blank
		}

		internal override void OnUpdate()
		{
			// intentionally left blank
		}

		#endregion
	}

	/// <summary>
	/// Represents player state, when player is walking towards given point
	/// </summary>
	internal class WalkingState : PlayerState
	{
		#region Fields

		/// <summary>
		/// Final destination of walking
		/// </summary>
		private readonly Vector2 _destination;

		#endregion

		#region Properties

		/// <summary>
		/// Relative position of the destination to player position
		/// </summary>
		internal Vector2 RelativePosition => _destination - Player.Position;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs base walking state
		/// </summary>
		/// <param name="player">Reference to the <see cref="PlayerController"/> owner object</param>
		/// <param name="destination"><see cref="Vector2"/> specifying final movement destination</param>
		internal WalkingState( PlayerController player, Vector2 destination ) : base( player )
		{
			_destination = destination;
			Face( RelativePosition );

			Player.Animator.SetTrigger( Player.MOVE_ANIMATION_TRIGGER );
		}

		#endregion

		#region Overrides

		internal override void OnCollision( Collision2D collision )
		{
			StopMovement();
		}

		internal override void OnUpdate()
		{
			Move();
		}

		#endregion

		#region Functions

		/// <summary>
		/// Turns player character (if necessary) so they will end up facing destination position
		/// </summary>
		/// <param name="relativePosition">Relative position of destination to player character's location</param>
		private void Face( Vector2 relativePosition )
		{
			var scale = Player.gameObject.transform.localScale;
			var facingLeft = scale.x > 0;

			if( ( facingLeft && relativePosition.x > 0 ) || ( !facingLeft && relativePosition.x < 0 ) )
			{
				Player.gameObject.transform.localScale = new Vector3( -1 * scale.x, 1f, 1f );
			}
		}

		/// <summary>
		/// Moves player based on their speed towards destination
		/// </summary>
		private void Move()
		{
			var finalVelocity = RelativePosition;
			if( RelativePosition.magnitude > Player.Speed * Time.deltaTime )
			{
				finalVelocity = RelativePosition.normalized * Player.Speed * Time.deltaTime;
			}

			if( finalVelocity.magnitude > 0 )
			{
				Player.gameObject.transform.Translate( finalVelocity );
			}
			else
			{
				StopMovement();
			}
		}

		/// <summary>
		/// Stops currently active movement of the player
		/// </summary>
		private void StopMovement()
		{
			Player.ChangeState( new IdleState( Player ) );
		}

		#endregion
	}

	#endregion

	/// <summary>
	/// Defines the behavior of the player sprite
	/// </summary>
	[RequireComponent( typeof( Animator ) )]
	public class PlayerController : MonoBehaviour, IGestureListener
	{
		#region Constants

		/// <summary>
		/// Name of the animation trigger for movement
		/// </summary>
		internal string MOVE_ANIMATION_TRIGGER = "Moves";

		#endregion

		#region Fields

		/// <summary>
		/// Timer to track how long since the last collision started.
		/// </summary>
		private float _collisionTimer;

		/// <summary>
		/// Flag to set to disable transitioning to walking state
		/// </summary>
		private bool _walkingAllowed = true;

		#endregion

		#region Properties

		/// <summary>
		/// Object handling gesture recognition
		/// </summary>
		public GestureHandler GestureHandler;

		/// <summary>
		/// Speed by which the player character moves
		/// </summary>
		public float Speed;

		/// <summary>
		/// How long a continuous collision is tolerated before the state machine reacts.
		/// </summary>
		public float collisionTimerThreshold;

		/// <summary>
		/// Current state of the player
		/// </summary>
		internal PlayerState State { get; private set; }

		/// <summary>
		/// Animator component of player object
		/// </summary>
		internal Animator Animator { get; private set; }

		/// <summary>
		/// Current position of the player
		/// </summary>
		internal Vector2 Position => gameObject.transform.position;

		#endregion

		#region Overrides

		private void Awake()
		{
			GestureHandler.RegisterListener( this );

			Animator = GetComponent<Animator>();

			State = new IdleState( this );
		}

		private void Update()
		{
			State.OnUpdate();
		}

		private void OnCollisionEnter2D( Collision2D collision )
		{
			_collisionTimer = 0;
		}

		private void OnCollisionStay2D( Collision2D collision )
		{
			_collisionTimer += Time.deltaTime;
			if(_collisionTimer >= collisionTimerThreshold)
			{
				_collisionTimer = 0;
				State.OnCollision( collision );
			}
		}

		#endregion

		#region Functions

		/// <summary>
		/// Changes player state to given one
		/// </summary>
		/// <param name="state">New state of player</param>
		internal void ChangeState( PlayerState state )
		{
			State = state;
			State.OnUpdate();
		}

		/// <summary>
		/// Allows/disallows transition to the walking state
		/// </summary>
		/// <param name="flag">Whether to allow walking or not</param>
		public void AllowWalking( bool flag )
		{
			_walkingAllowed = flag;
			if( flag == false && State.GetType() == typeof( WalkingState ) )
			{
				ChangeState( new IdleState( this ) );
			}
		}

		#endregion

		#region IGestureListener

		public void OnGestureStart( Gesture gesture )
		{
			switch( gesture.Type )
			{
				case GestureType.Tap:
					var origin = ((Tap) gesture).Origin;
					var destination = Camera.main.ScreenToWorldPoint( origin );

					if( _walkingAllowed )
					{
						ChangeState( new WalkingState( this, destination ) );
					}
					break;
			}
		}

		public void OnGestureEnd( Gesture gesture )
		{
			// intentionally left blank
		}

		#endregion
	}
}
