using UnityEngine;
using System.Collections;

public class NPC : MonoBehaviour {

	//Fields
	public static float maximumSeekVelocity = 15f, maximumRotationVelocity = 2f, 
		maximumFleeVelocity = 10f, maximumAcceleration = 0.05f, maxinumRotationAcceleration = 0.01f;
	protected float currentVelocity = 0, currentRotationVelocity = 0, currentAcceleration = 0.05f;
	Vector3 directionVector = new Vector3 (0, 0, 0);
	Vector3 playerDistance;
	Vector2 worldSize = new Vector2(70, 32.5f);
	public enum Gamestate { TAGGED, UNTAGGED, FROZEN }
	public Gamestate gamestate;
	public enum MovementType { KINEMATIC, STEERING }
	public enum ChaseType { SEEK, PURSUIT }
	public enum EvadeType { FLEE, EVADE }
	public MovementType movementType;
	public ChaseType chaseType;
	public EvadeType evadeType;
	float angle;

	//New Fields
	public Vector3 totalVelocity;


	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	}

	//Wrapper funtion for chasing the target
	public void Chase(NPC target) {
		float orientationAngle = Vector3.Angle (transform.forward.normalized, (target.transform.position - transform.position).normalized);
		print (orientationAngle);

		if (orientationAngle < 30 || (target.transform.position - transform.position).magnitude < 15) {
			switch (movementType) {
			case(MovementType.KINEMATIC):
				if (chaseType == ChaseType.SEEK) {
					Kinematic_Seek (target);
				}
				else if (chaseType == ChaseType.PURSUIT) {
					Kinematic_Pursuit (target);
				}
				break;
			case(MovementType.STEERING):
				if (chaseType == ChaseType.SEEK) {
					Steering_Seek (target);
				}
				else if (chaseType == ChaseType.PURSUIT) {
					Steering_Pursuit (target);
				}
				break;
			default:
				break;
			}
		}
		else {
			switch (movementType) {
			case (MovementType.KINEMATIC):
				rotateTowards (target.transform.position);
				break;
			case (MovementType.STEERING):
				if (Steering_Stop ()) {
					rotateTowards (target.transform.position);
				}
				break;
			default:
				break;
			}
		}
	}

	//Wrapper function for fleeing from the target
	public void Avoid (NPC target) {
		float orientationAngle = Vector3.Angle (transform.forward.normalized, (transform.position - target.transform.position).normalized);
		print (orientationAngle);
		
		if (orientationAngle < 30 || (target.transform.position - transform.position).magnitude > 18) {
			switch (movementType) {
			case(MovementType.KINEMATIC):
				if (evadeType == EvadeType.FLEE) {
					Kinematic_Flee (target);
				}
				else if (evadeType == EvadeType.EVADE) {
					Kinematic_Evade (target);
				}
				break;
			case(MovementType.STEERING):
				if (evadeType == EvadeType.FLEE) {
					Steering_Flee (target);
				}
				else if (evadeType == EvadeType.EVADE) {
					Steering_Evade (target);
				}
				break;
			default:
				break;
			}
		}
		else if (orientationAngle < 30 || (target.transform.position - transform.position).magnitude < 18) {
			Quaternion prevLook = transform.rotation;
			switch (movementType) {
			case(MovementType.KINEMATIC):
				if (evadeType == EvadeType.FLEE) {
					Kinematic_Flee (target);
				}
				else if (evadeType == EvadeType.EVADE) {
					Kinematic_Evade (target);
				}
				break;
			case(MovementType.STEERING):
				if (evadeType == EvadeType.FLEE) {
					Steering_Flee (target);
				}
				else if (evadeType == EvadeType.EVADE) {
					Steering_Evade (target);
				}
				break;
			default:
				break;
			}
			transform.rotation = prevLook;
		}
		else {
			switch (movementType) {
			case (MovementType.KINEMATIC):
				rotateTowards ((transform.position - target.transform.position) + transform.position);
				break;
			case (MovementType.STEERING):
				if (Steering_Stop ()) {
					rotateTowards ((transform.position - target.transform.position) + transform.position);
				}
				break;
			default:
				break;
			}
		}
	}

	//Wander around with the kinematic wander formula
	public void Wander () {
		float rotationDirection = Random.Range (-0.5f, 0.5f);
		angle = angle + maximumRotationVelocity * rotationDirection * Time.deltaTime;
		if (angle > 3.5f) {
						angle = 3.5f;
				} else if (angle < -3.5f) {
						angle = -3.5f;
				}
		transform.Rotate (0, angle, 0);
		Vector3 nextPosition = transform.position + maximumFleeVelocity * Time.deltaTime * transform.forward.normalized;
//		nextPosition.Normalize ();
//		nextPosition.x /= 4f;
//		nextPosition.y /= 4f;
//		nextPosition.z /= 4f;
		transform.position = nextPosition;
	}

	//Wander around with the steering wander formula
	public void Steering_Wander () {
		float rotationDirection = Random.Range (-1.5f, 1.5f);
		//Update the current velocities with the accelerations
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumFleeVelocity);
		//Calculate and clamp the rotation angle with the current rotation velocity
		angle = angle + currentRotationVelocity * rotationDirection * Time.deltaTime;
		if (angle > 3.5f) {
			angle = 3.5f;
		} else if (angle < -3.5f) {
			angle = -3.5f;
		}
		transform.Rotate (0, angle, 0);
		Vector3 nextPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = nextPosition;
	}

	//Chase the target with the kinematic seek formula
	public void Kinematic_Seek (NPC target) {
		//Find the direction vector based on the target's position
		directionVector = (target.transform.position - transform.position);
		directionVector.Normalize ();
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (maximumSeekVelocity * Time.deltaTime) * directionVector;
		transform.position = newPosition;
	}

	//Chase the target with the kinematic arrival formula
	public void Kinematic_Arrive (NPC target) {
		//Find the direction vector based on the target's position
		directionVector = (target.transform.position - transform.position);
		directionVector.Normalize ();
		//Find the current velocity by using the T2T method with the target's position
		currentVelocity = (maximumFleeVelocity * (target.transform.position - transform.position).magnitude / 15f);
		if (currentVelocity > maximumFleeVelocity)
			currentVelocity = maximumFleeVelocity;
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		//Find the next position by: p' = p + v(direction vector)t
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * directionVector;
		//Move to the next position
		transform.position = newPosition;
	}

	//Pursuit the target with the kinematic pursuit formula
	public void Kinematic_Pursuit (NPC target) {
		float estimatedArrivalTime = (target.transform.position - transform.position).magnitude / maximumSeekVelocity;
		Vector3 nextTargetPosition = target.transform.position + (NPC.maximumFleeVelocity * estimatedArrivalTime) * target.transform.forward.normalized;
		directionVector = (nextTargetPosition - transform.position);
		directionVector.Normalize ();
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		Vector3 newPosition = transform.position + (maximumSeekVelocity * Time.deltaTime) * directionVector;
		transform.position = newPosition;
	}
	
	//Flee from the target accoding to the kinematic flee formula
	public void Kinematic_Flee (NPC target) {
		directionVector = (transform.position - target.transform.position);
		directionVector.Normalize ();
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		Vector3 newPosition = transform.position + (maximumFleeVelocity * Time.deltaTime) * directionVector;
		transform.position = newPosition;
	}
	
	//Evade the target with the kinematic evade formula
	public void Kinematic_Evade (NPC target) {
		float estimatedArrivalTime = (target.transform.position - transform.position).magnitude / maximumSeekVelocity;
		Vector3 nextTargetPosition = target.transform.position + (NPC.maximumSeekVelocity * estimatedArrivalTime) * target.transform.forward.normalized;
		directionVector = (transform.position - nextTargetPosition);
		directionVector.Normalize ();
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		Vector3 newPosition = transform.position + (maximumFleeVelocity * Time.deltaTime) * directionVector;
		transform.position = newPosition;
	}



	//Chase the target using the steering seek formula
	public void Steering_Seek (NPC target) {
		//Find the direction vector based on the target's position
		directionVector = (target.transform.position - transform.position);
		directionVector.Normalize ();
		//Find the current velocity
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumSeekVelocity);
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = newPosition;
	}

	//Chase the target with the steering arrive formula
	public void Steering_Arrive (NPC target) {
		//Find the direction vector based on the target's position
		directionVector = (target.transform.position - transform.position);
		directionVector.Normalize ();
		//Find the current rotation velocity
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		//Create a goal velocity that is proportional to the distance to the target (interpolated from 0 to max)
		float goalVelocity = maximumSeekVelocity * ((target.transform.position - transform.position).magnitude / 15f);
		currentVelocity = Mathf.Min (currentVelocity + currentAcceleration, maximumFleeVelocity);
		//Calculate the current acceleration based on the goal velocity and the current velocity
		currentAcceleration = Mathf.Min ((goalVelocity - currentVelocity) / 2, maximumAcceleration);
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = newPosition;
	}
	
	//Pursuit the target with the steering pursuit formula
	public void Steering_Pursuit (NPC target) {
		float estimatedArrivalTime = (target.transform.position - transform.position).magnitude / maximumSeekVelocity;
		Vector3 nextTargetPosition = target.transform.position + (NPC.maximumFleeVelocity * estimatedArrivalTime) * target.transform.forward.normalized;
		//Find the direction vector based on the target's future position
		directionVector = (nextTargetPosition - transform.position);
		directionVector.Normalize ();
		//Find the current velocity
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumSeekVelocity);
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = newPosition;
	}

	//Flee from the target with the steering flee formula
	public void Steering_Flee (NPC target) {
		//Find the direction vector based on the target's position
		directionVector = (transform.position - target.transform.position);
		directionVector.Normalize ();
		//Find the current velocity
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumFleeVelocity);
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = newPosition;
	}

	//Evade from the target with the steering evade formula
	public void Steering_Evade (NPC target) {
		float estimatedArrivalTime = (target.transform.position - transform.position).magnitude / maximumSeekVelocity / 2f; //reduced estimated time for better gameplay
		Vector3 nextTargetPosition = target.transform.position + (NPC.maximumSeekVelocity * estimatedArrivalTime) * target.transform.forward.normalized;
		//Find the direction vector based on the target's future position
		directionVector = (transform.position - nextTargetPosition);
		directionVector.Normalize ();
		//Find the current velocity
		currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
		currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumFleeVelocity);
		//Interpolate the orientation of the NPC object
		Quaternion targetRotation = Quaternion.LookRotation (directionVector);
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
		//Update the position
		Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
		transform.position = newPosition;
	}

	//Stop the movement if in steering mode
	public bool Steering_Stop() {
		currentVelocity = currentVelocity - maximumAcceleration - 0.1f; //changed for assignment 2
		if (currentVelocity > 0) {
			Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
			transform.position = newPosition;
			return false;
		}
		else {
			currentVelocity = 0;
			return true;
		}
	}



	//Check object's collision with the world's boundaries
	public void CheckBoundaries () {
		Vector3 prevPosition = transform.position;
		if (transform.position.x > worldSize.x) {
			transform.position = new Vector3(-worldSize.x, prevPosition.y, prevPosition.z);
			prevPosition = transform.position;
		}
		else if (transform.position.x < -worldSize.x) {
			transform.position = new Vector3(worldSize.x, prevPosition.y, prevPosition.z);
			prevPosition = transform.position;
		}
		
		if (transform.position.z > worldSize.y) {
			transform.position = new Vector3(prevPosition.x, prevPosition.y, -worldSize.y);
			prevPosition = transform.position;
		}
		else if (transform.position.z < -worldSize.y) {
			transform.position = new Vector3(prevPosition.x, prevPosition.y, worldSize.y);
			prevPosition = transform.position;
		}
	}

	//Turn towards the target position
	public bool rotateTowards(Vector3 targetPosition) {
		Quaternion targetRotation = Quaternion.LookRotation (targetPosition - transform.position);
		
		transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, maximumRotationVelocity * Time.deltaTime);
		if (transform.rotation == targetRotation) {
			return true;
		} 
		else {
			return false;
		}
	}



	//Arrive to a node in path following
	public void Steering_Arrive (Vector3 targetPosition, bool endNode) {
		if (endNode) {
			//Find the direction vector based on the target's position
			directionVector = (targetPosition - transform.position);
			directionVector.Normalize ();
			//Find the current rotation velocity
			currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
			//Create a goal velocity that is proportional to the distance to the target (interpolated from 0 to max)
			float goalVelocity = maximumSeekVelocity * ((targetPosition - transform.position).magnitude / 15f);
			currentVelocity = Mathf.Min (currentVelocity + currentAcceleration, maximumFleeVelocity);
			//Calculate the current acceleration based on the goal velocity and the current velocity
			currentAcceleration = Mathf.Min ((goalVelocity - currentVelocity) / 2, maximumAcceleration);
			//Interpolate the orientation of the NPC object
			Quaternion targetRotation = Quaternion.LookRotation (directionVector);
			transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
			//Update the position
			Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
			transform.position = newPosition;
		}
		else {
			//Find the direction vector based on the target's position
			directionVector = (targetPosition - transform.position);
			directionVector.Normalize ();
			//Find the current velocity
			currentRotationVelocity = Mathf.Min (currentRotationVelocity + maxinumRotationAcceleration, maximumRotationVelocity);
			currentVelocity = Mathf.Min (currentVelocity + maximumAcceleration, maximumSeekVelocity - 10);
			//Interpolate the orientation of the NPC object
			Quaternion targetRotation = Quaternion.LookRotation (directionVector);
			transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, currentRotationVelocity * Time.deltaTime);
			//Update the position
			Vector3 newPosition = transform.position + (currentVelocity * Time.deltaTime) * transform.forward.normalized;
			transform.position = newPosition;
		}
	}


	protected bool waitTOTurnTagged = false;
	protected float turnToTaggedTimer = 0;
	//Add a timer for turning into tagged
	public void TurnToTagged () {
		waitTOTurnTagged = true;
		//gamestate = Gamestate.TAGGED;
		turnToTaggedTimer += Time.deltaTime;
		if (turnToTaggedTimer > 1) {
			turnToTaggedTimer = 0;
			waitTOTurnTagged = false;
			gamestate = Gamestate.TAGGED;
		}

	}
}
