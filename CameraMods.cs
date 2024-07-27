using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using GlobalEnums;
using oncamc = On.CameraController;
using oncamt = On.CameraTarget;
using oncaml = On.CameraLockArea;

namespace NoTransitions {
    class CameraMods {
#pragma warning disable IDE1006 // Naming Styles
		public static void Initialize() {
            oncamc.LateUpdate += lateUpdate;
			oncamc.LockToArea += lockToArea;
			oncamc.ReleaseLock += releaseLock;
			oncamc.KeepWithinSceneBounds_Vector3 += keepWithinSceneBounds3;
			oncamc.KeepWithinSceneBounds_Vector2 += keepWithinSceneBounds2;
			oncamc.IsAtSceneBounds += isAtSceneBounds;
			oncamc.IsAtHorizontalSceneBounds += isAtHorizontalSceneBounds;
			oncamc.IsTouchingSides += isTouchingSides;
			oncamc.KeepWithinLockBounds += keepWithinLockBounds;

			oncamt.Update += update;
			oncamt.EnterLockZone += enterLockZone;
			oncamt.EnterLockZoneInstant += enterLockZoneInstant;
			oncamt.ExitLockZone += exitLockZone;
			oncamt.PositionToStart += positionToStart;

			oncaml.ValidateBounds += validateBounds;
		}

        private static void lateUpdate(oncamc.orig_LateUpdate orig, CameraController self) {
			float x = self.transform.position.x;
			float y = self.transform.position.y;
			float z = self.transform.position.z;
			if(GameManager.instance.IsGameplayScene() && self.mode != CameraController.CameraMode.FROZEN) {
				if(HeroController.instance.cState.lookingUp) {
					self.lookOffset = HeroController.instance.transform.position.y - self.camTarget.transform.position.y + 6f;
				}
				else if(HeroController.instance.cState.lookingDown) {
					self.lookOffset = HeroController.instance.transform.position.y - self.camTarget.transform.position.y - 6f;
				}
				else {
					self.lookOffset = 0f;
				}
				callPrivateMethod(self, "UpdateTargetDestinationDelta");
				Vector3 vector = self.cam.WorldToViewportPoint(self.camTarget.transform.position);
				Vector3 vector2 = new Vector3(getPrivateVariable_float(self,"targetDeltaX"), getPrivateVariable_float(self,"targetDeltaY"), 0f) - self.cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, vector.z));
				self.destination = new Vector3(x + vector2.x, y + vector2.y, z);
				if(self.mode == CameraController.CameraMode.FOLLOWING || self.mode == CameraController.CameraMode.LOCKED) {
					self.destination = self.KeepWithinSceneBounds(self.destination);
				}
				Vector3 velocityX = getPrivateVariable_vector3(self, "velocityX");
				Vector3 velocityY = getPrivateVariable_vector3(self, "velocityY");
				Vector3 vector3 = Vector3.SmoothDamp(self.transform.position, new Vector3(self.destination.x, y, z), ref velocityX, self.dampTimeX);
				Vector3 vector4 = Vector3.SmoothDamp(self.transform.position, new Vector3(x, self.destination.y, z), ref velocityY, self.dampTimeY);
				setPrivateVariable(self, "velocityX", velocityX);
				setPrivateVariable(self, "velocityY", velocityY);
				self.transform.SetPosition2D(vector3.x, vector4.y);
				Vector3 velocity = getPrivateVariable_vector3(self, "velocity");
				float maxVelocityCurrent = getPrivateVariable_float(self, "maxVelocityCurrent");
				if(velocity.magnitude > maxVelocityCurrent) {
					setPrivateVariable(self, "velocity", velocity.normalized * maxVelocityCurrent);
				}
			}
			if(GameManager.instance.IsGameplayScene()) {
				float startLockedTimer = getPrivateVariable_float(self, "startLockedTimer");
				if(startLockedTimer > 0f) {
					setPrivateVariable(self, "startLockedTimer", startLockedTimer - Time.deltaTime);
				}
			}
		}

		private static void lockToArea(oncamc.orig_LockToArea orig, CameraController self, CameraLockArea lockArea) {}

		private static void releaseLock(oncamc.orig_ReleaseLock orig, CameraController self, CameraLockArea lockarea) {}

		private static Vector3 keepWithinSceneBounds3(oncamc.orig_KeepWithinSceneBounds_Vector3 orig, CameraController self, Vector3 targetDest) {
			self.atSceneBounds = false;
			self.atHorizontalSceneBounds = false;
			return targetDest;
		}

		private static Vector2 keepWithinSceneBounds2(oncamc.orig_KeepWithinSceneBounds_Vector2 orig, CameraController self, Vector2 targetDest) {
			self.atSceneBounds = false;
			return targetDest;
		}

		private static bool isAtSceneBounds(oncamc.orig_IsAtSceneBounds orig, CameraController self, Vector2 targetDest) {
			return false;
		}

		private static bool isAtHorizontalSceneBounds(oncamc.orig_IsAtHorizontalSceneBounds orig, CameraController self, Vector2 targetDest, out bool leftSide) {
			leftSide = false;
			return false;
		}

		private static bool isTouchingSides(oncamc.orig_IsTouchingSides orig, CameraController self, float x) {
			return false;
		}

		private static Vector2 keepWithinLockBounds(oncamc.orig_KeepWithinLockBounds orig, CameraController self, Vector2 targetDest) {
			return new Vector2(targetDest.x, targetDest.y);
		}

		private static void update(oncamt.orig_Update orig, CameraTarget self) {
			if(self.hero_ctrl == null || !GameManager.instance.IsGameplayScene()) {
				self.mode = CameraTarget.TargetMode.FREE;
				return;
			}
			if(GameManager.instance.IsGameplayScene()) {
				float num = self.transform.position.x;
				float num2 = self.transform.position.y;
				float z = self.transform.position.z;
				float x = self.hero_ctrl.transform.position.x;
				float y = self.hero_ctrl.transform.position.y;
				Vector3 position = self.hero_ctrl.transform.position;
				Vector3 heroPrevPosition = getPrivateVariable_vector3(self, "heroPrevPosition");
				float snapDistance = getPrivateVariable_float(self, "snapDistance");
				Vector3 velocityX = getPrivateVariable_vector3(self, "velocityX");
				Vector3 velocityY = getPrivateVariable_vector3(self, "velocityY");
				float dampTimeX = getPrivateVariable_float(self, "dampTimeX");
				float dampTimeY = getPrivateVariable_float(self, "dampTimeY");
				if(self.mode == CameraTarget.TargetMode.FOLLOW_HERO) {
					callPrivateMethod(self, "SetDampTime");
					self.destination = self.hero_ctrl.transform.position;
					if(!self.fallStick && self.fallCatcher <= 0f) {
						self.transform.position = new Vector3(Vector3.SmoothDamp(self.transform.position, new Vector3(self.destination.x, self.transform.position.y, z), ref velocityX, dampTimeX).x, Vector3.SmoothDamp(self.transform.position, new Vector3(self.transform.position.x, self.destination.y, z), ref velocityY, dampTimeY).y, z);
					}
					else {
						self.transform.position = new Vector3(Vector3.SmoothDamp(self.transform.position, new Vector3(self.destination.x, self.transform.position.y, z), ref velocityX, dampTimeX).x, self.transform.position.y, z);
					}
					num = self.transform.position.x;
					num2 = self.transform.position.y;
					z = self.transform.position.z;
					if((heroPrevPosition.x < num && x > num) || (heroPrevPosition.x > num && x < num) || (num >= x - snapDistance && num <= x + snapDistance)) {
						self.stickToHeroX = true;
					}
					if((heroPrevPosition.y < num2 && y > num2) || (heroPrevPosition.y > num2 && y < num2) || (num2 >= y - snapDistance && num2 <= y + snapDistance)) {
						self.stickToHeroY = true;
					}
					if(self.stickToHeroX) {
						self.transform.SetPositionX(x);
						num = x;
					}
					if(self.stickToHeroY) {
						self.transform.SetPositionY(y);
						num2 = y;
					}
				}
				if(self.mode == CameraTarget.TargetMode.LOCK_ZONE) {
					callPrivateMethod(self, "SetDampTime");
					self.destination = self.hero_ctrl.transform.position;
					if(!self.fallStick && self.fallCatcher <= 0f) {
						self.transform.position = new Vector3(Vector3.SmoothDamp(self.transform.position, new Vector3(self.destination.x, num2, z), ref velocityX, dampTimeX).x, Vector3.SmoothDamp(self.transform.position, new Vector3(num, self.destination.y, z), ref velocityY, dampTimeY).y, z);
					}
					else {
						self.transform.position = new Vector3(Vector3.SmoothDamp(self.transform.position, new Vector3(self.destination.x, num2, z), ref velocityX, dampTimeX).x, num2, z);
					}
					num = self.transform.position.x;
					num2 = self.transform.position.y;
					z = self.transform.position.z;
					if((heroPrevPosition.x < num && x > num) || (heroPrevPosition.x > num && x < num) || (num >= x - snapDistance && num <= x + snapDistance)) {
						self.stickToHeroX = true;
					}
					if((heroPrevPosition.y < num2 && y > num2) || (heroPrevPosition.y > num2 && y < num2) || (num2 >= y - snapDistance && num2 <= y + snapDistance)) {
						self.stickToHeroY = true;
					}
					if(self.stickToHeroX) {
						self.transform.SetPositionX(x);
						num = x;
					}
					if(self.stickToHeroY) {
						self.transform.SetPositionY(y);
					}
				}
				setPrivateVariable(self, "velocityX", velocityX);
				setPrivateVariable(self, "velocityY", velocityY);
				if(self.hero_ctrl != null) {
					if(self.hero_ctrl.cState.facingRight) {
						if(self.xOffset < self.xLookAhead) {
							self.xOffset += Time.deltaTime * 6f;
						}
					}
					else if(self.xOffset > -self.xLookAhead) {
						self.xOffset -= Time.deltaTime * 6f;
					}
					if(self.xOffset < -self.xLookAhead) {
						self.xOffset = -self.xLookAhead;
					}
					if(self.xOffset > self.xLookAhead) {
						self.xOffset = self.xLookAhead;
					}
					if(self.xOffset < -self.xLookAhead) {
						self.xOffset = -self.xLookAhead;
					}
					if(self.xOffset > self.xLookAhead) {
						self.xOffset = self.xLookAhead;
					}
					if(self.hero_ctrl.cState.dashing && (self.hero_ctrl.current_velocity.x > 5f || self.hero_ctrl.current_velocity.x < -5f)) {
						if(self.hero_ctrl.cState.facingRight) {
							self.dashOffset = self.dashLookAhead;
						}
						else {
							self.dashOffset = -self.dashLookAhead;
						}
					}
					else if(self.superDashing) {
						if(self.hero_ctrl.cState.facingRight) {
							self.dashOffset = self.superDashLookAhead;
						}
						else {
							self.dashOffset = -self.superDashLookAhead;
						}
					}
					else {
						self.dashOffset = 0f;
					}
					setPrivateVariable(self, "heroPrevPosition", self.hero_ctrl.transform.position);
				}
				if(self.hero_ctrl != null && !self.hero_ctrl.cState.falling) {
					self.fallCatcher = 0f;
					self.fallStick = false;
				}
				if(self.mode == CameraTarget.TargetMode.FOLLOW_HERO || self.mode == CameraTarget.TargetMode.LOCK_ZONE) {
					if(self.hero_ctrl.cState.falling && self.cameraCtrl.transform.position.y > y + 0.1f && !self.fallStick && !self.hero_ctrl.cState.transitioning) {
						self.cameraCtrl.transform.SetPositionY(self.cameraCtrl.transform.position.y - self.fallCatcher * Time.deltaTime);
						if(self.fallCatcher < 25f) {
							self.fallCatcher += 80f * Time.deltaTime;
						}
						if(self.cameraCtrl.transform.position.y < self.hero_ctrl.transform.position.y + 0.1f) {
							self.fallStick = true;
						}
						self.transform.SetPositionY(self.cameraCtrl.transform.position.y);
						num2 = self.cameraCtrl.transform.position.y;
					}
					if(self.fallStick) {
						self.fallCatcher = 0f;
						self.cameraCtrl.transform.SetPositionY(self.hero_ctrl.transform.position.y + 0.1f);
						self.transform.SetPositionY(self.cameraCtrl.transform.position.y);
						num2 = self.cameraCtrl.transform.position.y;
					}
				}
				if(self.quaking) {
					num2 = self.hero_ctrl.transform.position.y;
					self.transform.SetPositionY(num2);
				}
			}
		}

		private static void enterLockZone(oncamt.orig_EnterLockZone orig, CameraTarget self, float xLockMin_var, float xLockMax_var, float yLockMin_var, float yLockMax_var) {
			self.mode = CameraTarget.TargetMode.LOCK_ZONE;
			float x = self.transform.position.x;
			float y = self.transform.position.y;
			Vector3 position = self.transform.position;
			float x2 = self.hero_ctrl.transform.position.x;
			float y2 = self.hero_ctrl.transform.position.y;
			Vector3 position2 = self.hero_ctrl.transform.position;
			setPrivateVariable(self, "slowTimer", self.slowTime);
			float snapDistance = getPrivateVariable_float(self, "snapDistance");
			if(x >= x2 - snapDistance && x <= x2 + snapDistance) {
				self.stickToHeroX = true;
			}
			else {
				self.stickToHeroX = false;
			}
			if(y >= y2 - snapDistance && y <= y2 + snapDistance) {
				self.stickToHeroY = true;
				return;
			}
			self.stickToHeroY = false;
		}

		private static void enterLockZoneInstant(oncamt.orig_EnterLockZoneInstant orig, CameraTarget self, float xLockMin_var, float xLockMax_var, float yLockMin_var, float yLockMax_var) {
			self.mode = CameraTarget.TargetMode.LOCK_ZONE;
			self.stickToHeroX = true;
			self.stickToHeroY = true;
		}

		private static void exitLockZone(oncamt.orig_ExitLockZone orig, CameraTarget self) {
			if(self.mode == CameraTarget.TargetMode.FREE) {
				return;
			}
			if(self.hero_ctrl.cState.hazardDeath || self.hero_ctrl.cState.dead || (self.hero_ctrl.transitionState != HeroTransitionState.WAITING_TO_TRANSITION && self.hero_ctrl.transitionState != HeroTransitionState.WAITING_TO_ENTER_LEVEL)) {
				self.mode = CameraTarget.TargetMode.FREE;
			}
			else {
				self.mode = CameraTarget.TargetMode.FOLLOW_HERO;
			}
			setPrivateVariable(self, "slowTimer", self.slowTime);
			self.stickToHeroX = false;
			self.stickToHeroY = false;
			self.fallStick = false;
			float snapDistance = getPrivateVariable_float(self, "snapDistance");
			if(self.hero_ctrl != null) {
				if(self.transform.position.x >= self.hero_ctrl.transform.position.x - snapDistance && self.transform.position.x <= self.hero_ctrl.transform.position.x + snapDistance) {
					self.stickToHeroX = true;
				}
				else {
					self.stickToHeroX = false;
				}
				if(self.transform.position.y >= self.hero_ctrl.transform.position.y - snapDistance && self.transform.position.y <= self.hero_ctrl.transform.position.y + snapDistance) {
					self.stickToHeroY = true;
					return;
				}
				self.stickToHeroY = false;
			}
		}

		private static void positionToStart(oncamt.orig_PositionToStart orig, CameraTarget self) {
			float x = self.transform.position.x;
			Vector3 position = self.transform.position;
			float x2 = self.hero_ctrl.transform.position.x;
			float y = self.hero_ctrl.transform.position.y;
			setPrivateVariable(self, "velocityX", Vector3.zero);
			setPrivateVariable(self, "velocityY", Vector3.zero);
			self.destination = self.hero_ctrl.transform.position;
			if(self.hero_ctrl.cState.facingRight) {
				self.xOffset = 1f;
			}
			else {
				self.xOffset = -1f;
			}
			if(self.xOffset < -self.xLookAhead) {
				self.xOffset = -self.xLookAhead;
			}
			if(self.xOffset > self.xLookAhead) {
				self.xOffset = self.xLookAhead;
			}
			if(self.mode == CameraTarget.TargetMode.FOLLOW_HERO) {
				self.transform.position = self.cameraCtrl.KeepWithinSceneBounds(self.destination);
			}
			else if(self.mode == CameraTarget.TargetMode.LOCK_ZONE) {
				self.transform.position = self.destination;
			}
			setPrivateVariable(self, "heroPrevPosition", self.hero_ctrl.transform.position);
		}

		private static bool validateBounds(oncaml.orig_ValidateBounds orig, CameraLockArea self) {
			return true;
		}

		/////////////////////////////////////////////////////////////

		public static void setPrivateVariable(CameraController gameObject, string variableName, float value) {
			FieldInfo field = typeof(CameraController).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			if(gameObject != null) {
				field.SetValue(gameObject, value);
			}
		}

		public static void setPrivateVariable(CameraController gameObject, string variableName, Vector3 value) {
            FieldInfo field = typeof(CameraController).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			if(gameObject != null) {
                field.SetValue(gameObject, value);
			}
		}

		public static void setPrivateVariable(CameraTarget gameObject, string variableName, float value) {
            FieldInfo field = typeof(CameraTarget).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
            if(gameObject != null) {
                field.SetValue(gameObject, value);
            }
		}

		public static void setPrivateVariable(CameraTarget gameObject, string variableName, Vector3 value) {
			FieldInfo field = typeof(CameraTarget).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			if(gameObject != null) {
				field.SetValue(gameObject, value);
			}
		}

		public static float getPrivateVariable_float(CameraTarget gameObject, string variableName) {
			FieldInfo field = typeof(CameraTarget).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			return (float)field.GetValue(gameObject);
		}

		public static Vector3 getPrivateVariable_vector3(CameraTarget gameObject, string variableName) {
			FieldInfo field = typeof(CameraTarget).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			return (Vector3)field.GetValue(gameObject);
		}

		public static float getPrivateVariable_float(CameraController gameObject, string variableName) {
            FieldInfo field = typeof(CameraController).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			return (float)field.GetValue(gameObject);
		}

		public static Vector3 getPrivateVariable_vector3(CameraController gameObject, string variableName) {
			FieldInfo field = typeof(CameraController).GetField(variableName, BindingFlags.NonPublic | BindingFlags.Instance);
			return (Vector3)field.GetValue(gameObject);
		}

		public static dynamic callPrivateMethod(CameraController gameObject, string functionName) {
			MethodInfo method = typeof(CameraController).GetMethod(functionName, BindingFlags.NonPublic | BindingFlags.Instance);
			if(gameObject != null) {
				return method.Invoke(gameObject,null);
			}
			return null;
		}

		public static dynamic callPrivateMethod(CameraTarget gameObject, string functionName) {
			MethodInfo method = typeof(CameraTarget).GetMethod(functionName, BindingFlags.NonPublic | BindingFlags.Instance);
			if(gameObject != null) {
				return method.Invoke(gameObject, null);
			}
			return null;
		}
	}
}
