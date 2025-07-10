
#if UNITY_EDITOR
using UnityEngine;
using System;
using UnityEngine.Animations;

namespace MivorTools.Extras.PlayControls {
	
	[AddComponentMenu("MivorTools/Extras/Play Controls",1)]
	[HelpURL("https://github.com/Jashepp/VRC-MivorTools")]
	public class PlayControls: MonoBehaviour, VRC.SDKBase.IEditorOnly {
		
   		[NonSerialized] [HideInInspector] private Rigidbody rigidBody = null;
		
		[SerializeField] public KeyCode holdToMove = KeyCode.Mouse1;
		[SerializeField] public KeyCode moveForwardKey = KeyCode.W;
		[SerializeField] public KeyCode moveLeftKey = KeyCode.A;
		[SerializeField] public KeyCode moveRightKey = KeyCode.D;
		[SerializeField] public KeyCode moveBackKey = KeyCode.S;
		[SerializeField] public KeyCode moveUpKey = KeyCode.Space;
		[SerializeField] public KeyCode moveDownKey = KeyCode.C;
		[SerializeField] public KeyCode moveBoostKey = KeyCode.LeftShift;
		[SerializeField] public KeyCode rotateLeftKey = KeyCode.Q;
		[SerializeField] public KeyCode rotateRightKey = KeyCode.E;
		[SerializeField] public KeyCode resetKey = KeyCode.Tab;
   		
		[NonSerialized] [HideInInspector] public bool mouseIsLocked = false;
		[NonSerialized] [HideInInspector] public bool mouseIsVisible = true;
		[SerializeField] public Vector3 rotationSensitivity = new Vector3( 170f, 170f, 90f );
		[SerializeField] public Vector3 movementAmountPos = new Vector3( 200f, 100f, 200f ); // X, Y, Z | Right, Up, Forward
		[SerializeField] public Vector3 movementAmountNeg = new Vector3( 200f, 100f, 200f ); // X, Y, Z | Left, Down, Back
		[SerializeField] public float movementBoost = 400f; // Shift Forward (additive on forward)
		[SerializeField] public bool useInertialDampeners = true;
		
		[NonSerialized] [HideInInspector] private GameObject originalPosition = null;
		
		// Start is called before the first frame update
		public void Start(){
			if(!enabled) return;
			Camera camera = this.GetComponent<Camera>() ?? this.GetComponentInChildren<Camera>();
			if(!camera) throw new Exception("Play Script must be on the Camera Object or one of its parent objects.");
			rigidBody = this.GetComponent<Rigidbody>();
			if(!rigidBody) rigidBody = this.gameObject.AddComponent<Rigidbody>();
			rigidBody.useGravity = false;
			rigidBody.mass = 1;
			rigidBody.drag = 1;
			rigidBody.angularDrag = 1;
			originalPosition = new GameObject(){ name="Starting Camera Position" };
			originalPosition.transform.SetParent(this.gameObject.transform.parent);
			originalPosition.transform.SetLocalPositionAndRotation(this.rigidBody.transform.position,this.rigidBody.transform.rotation);
			ParentConstraint parentConstraint = this.GetComponent<ParentConstraint>();
			if(parentConstraint) parentConstraint.enabled = false;
			LookAtConstraint lookAtConstraint = this.GetComponent<LookAtConstraint>();
			if(lookAtConstraint) lookAtConstraint.enabled = false;
			if(holdToMove<KeyCode.Mouse0 || holdToMove>KeyCode.Mouse6) holdToMove = KeyCode.Mouse1;
		}
		
		// Update is called once per frame
		public void Update(){
			if(!enabled || !rigidBody) return;
			
			// Right Mouse Button Held Down
			bool usingMouseRotation = Input.GetMouseButton(holdToMove-KeyCode.Mouse0);
			// Get Mouse Movement
			Vector2 mouseMove = GetMouseAxis(); // 0,0 starts at bottom left
			
			// Lock/Unlock Mouse
			if(usingMouseRotation && !mouseIsLocked){ Cursor.lockState = CursorLockMode.Locked; mouseIsLocked = true; mouseMove = Vector2.zero; }
			else if(!usingMouseRotation && mouseIsLocked){ Cursor.lockState = CursorLockMode.None; mouseIsLocked = false; }
			// Hide/Show Mouse Cursor
			if(usingMouseRotation && mouseIsVisible) Cursor.visible = mouseIsVisible = false;
			else if(!usingMouseRotation && !mouseIsVisible) Cursor.visible = mouseIsVisible = true;

			// Rotation
			Vector3 rotation = Vector3.zero;
			{
				// Y Axis
				if(usingMouseRotation) rotation.y += +mouseMove.x * rotationSensitivity.y;
				// X Axis
				if(usingMouseRotation) rotation.x += -mouseMove.y * rotationSensitivity.x;
				// Z Axis - Tilt Left
				if(Input.GetKey(rotateLeftKey)) rotation.z += +rotationSensitivity.z;
				// Z Axis - Tilt Right
				if(Input.GetKey(rotateRightKey)) rotation.z += -rotationSensitivity.z;
				// Apply Rotation
				if(rotation!=Vector3.zero){
					rotation *= Time.deltaTime;
					Quaternion rotationQ = Quaternion.Euler(rotation);
					rigidBody.MoveRotation(rigidBody.rotation * rotationQ);
					// rigidBody.AddRelativeTorque(rotation * Time.deltaTime);
				}
			}
			
			// Instant Stop & Reset
			if(Input.GetKeyDown(resetKey)){
				rigidBody.velocity = Vector3.zero;
				rigidBody.angularVelocity = Vector3.zero;
				if(originalPosition) rigidBody.transform.SetPositionAndRotation(originalPosition.transform.position,originalPosition.transform.rotation);
			}
		}
		
		// Used for Physics
		public void FixedUpdate(){
			if(!enabled || !rigidBody) return;

			// SpaceBody Reset Velocity
			resetVelocityChanges();
			
			// SpaceBody Movement
			Vector3 acceleration = Vector3.zero;
			{
				bool useLimits = true;
				// Forward
				if(Input.GetKey(moveForwardKey) || Input.GetKeyDown(moveForwardKey)) acceleration.z += +movementAmountPos.z;
				// Back
				if(Input.GetKey(moveBackKey) || Input.GetKeyDown(moveBackKey)) acceleration.z += -movementAmountNeg.z;
				// Right
				if(Input.GetKey(moveRightKey) || Input.GetKeyDown(moveRightKey)) acceleration.x += +movementAmountPos.x;
				// Left
				if(Input.GetKey(moveLeftKey) || Input.GetKeyDown(moveLeftKey)) acceleration.x += -movementAmountNeg.x;
				// Up
				if(Input.GetKey(moveUpKey) || Input.GetKeyDown(moveUpKey)) acceleration.y += +movementAmountPos.y;
				// Down
				if(Input.GetKey(moveDownKey) || Input.GetKeyDown(moveDownKey)) acceleration.y += -movementAmountNeg.y;
				// Boost Acceleration
				if(Input.GetKey(moveBoostKey) || Input.GetKeyDown(moveBoostKey)){ acceleration *= 2; useLimits = false; }
				// Acceleration
				applyAcceleration(Time.fixedDeltaTime,acceleration,useLimits);
			}

			// SpaceBody Inertial Dampeners
			if(useInertialDampeners) applyInertialDampeners(Time.fixedDeltaTime);
			
			// SpaceBody Apply Velocity Changes
			applyVelocityChanges();
			
		}
		
		public Vector2 GetMouseAxis(){
			return new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
		}
		
		[NonSerialized] public Vector3 localVelocityStart = Vector3.zero;
		[NonSerialized] public Vector3 localVelocityChange = Vector3.zero;
		[NonSerialized] public Vector3 velocityAcceleration = Vector3.zero; // Sum of accelerations
		[NonSerialized] public Vector3 velocityInertialDampeners = Vector3.zero; // Sum of all inertial dampeners
		public Vector3 calcNewVelocity(){
			return localVelocityStart + rigidBody.transform.TransformVector(localVelocityChange);
		}
		public void resetVelocityChanges(){
			localVelocityStart = rigidBody.velocity;
			localVelocityChange = Vector3.zero;
			velocityAcceleration = Vector3.zero;
			velocityInertialDampeners = Vector3.zero;
		}
		public void applyVelocityChanges(){
			rigidBody.AddRelativeForce(localVelocityChange);
		}
		
		public void applyAcceleration(float deltaTime,Vector3 acceleration,bool applyLimits=true){
			if(velocityAcceleration!=Vector3.zero) return;
			if(applyLimits){
				acceleration.x = Math.Min(Math.Max(acceleration.x,-movementAmountNeg.x),+movementAmountPos.x);
				acceleration.y = Math.Min(Math.Max(acceleration.y,-movementAmountNeg.y),+movementAmountPos.y);
				acceleration.z = Math.Min(Math.Max(acceleration.z,-movementAmountNeg.z),+movementAmountPos.z);
			}
			// Add Acceleration Force
			if(acceleration!=Vector3.zero){
				acceleration *= deltaTime;
				velocityAcceleration = acceleration;
				localVelocityChange += acceleration;
			}
		}
		
		public void applyInertialDampeners(float deltaTime){
			if(velocityInertialDampeners!=Vector3.zero) return;
			Vector3 dampCurrentVelocity = calcNewVelocity();
			Vector3 inertiaDampener = Vector3.zero;
			if(dampCurrentVelocity!=Vector3.zero){
				Vector3 relativeVelocity = rigidBody.transform.InverseTransformVector(dampCurrentVelocity);
				Vector3 maxMovementPos = movementAmountPos * deltaTime;
				Vector3 maxMovementNeg = movementAmountNeg * deltaTime;
				if(velocityAcceleration.x==0f) inertiaDampener.x = Math.Min(Math.Max(-relativeVelocity.x*2,-maxMovementNeg.x*2),+maxMovementPos.x*2);
				if(velocityAcceleration.y==0f) inertiaDampener.y = Math.Min(Math.Max(-relativeVelocity.y*2,-maxMovementNeg.y*2),+maxMovementPos.y*2);
				if(velocityAcceleration.z==0f) inertiaDampener.z = Math.Min(Math.Max(-relativeVelocity.z*2,-maxMovementNeg.z*2),+maxMovementPos.z*2);
				velocityInertialDampeners = inertiaDampener;
				localVelocityChange += inertiaDampener;
			}
		}

	}


}

#endif
