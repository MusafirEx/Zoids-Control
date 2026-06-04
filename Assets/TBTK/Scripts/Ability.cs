using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TBTK{
	

	public enum AbilityActionCamAnchor{
		Attacker,
		Target
	}

	public enum AbilityActionCamChain{
		Move,	//Move smoothly from this keyframe to the next keyframe
		Snap,	//Hold this keyframe, then snap to the next keyframe at its timestamp
		Follow	//Keep following this keyframe's anchor until the next timestamp
	}

	public enum AbilityActionCamStartMode{
		OnAbilityActivated,		//Timeline starts as soon as the ability routine starts
		OnAbilityAnimationStart	//Timeline starts only when Ability/Attack animation starts, after any pre-movement
	}

	[System.Serializable]
	public class AbilityActionCamKeyframe{
		public AbilityActionCamAnchor anchor=AbilityActionCamAnchor.Attacker;
		public Vector3 position=new Vector3(0, 3, -6);
		public Vector3 rotation=new Vector3(20, 0, 0);
		public float timeStamp=0f;
		public AbilityActionCamChain chain=AbilityActionCamChain.Move;
	}

	[System.Serializable]
	public class Ability : TBTKItem{
		
		public enum _AbilityType{ Generic, Teleport, SpawnUnit, Fusion, ChangeForm, Charge, Line, Cone, ScanFogOfWar, DeployBlock, None,  }
		
		public enum _TargetType{AllNode, AllUnit, HostileUnit, FriendlyUnit, EmptyNode}
		public enum _SkillRangeType{Distance, Melee}
		public enum _ImpactType{None, Negative, Positive}
		
		
		[HideInInspector] public Unit srcUnit;	//runtime attribute
		[HideInInspector] public int facID;		//runtime attribute
		[HideInInspector] public int index;		//runtime attribute
		public void Init(Unit unit, int idx){ srcUnit=unit; facID=srcUnit.facID; index=idx; isUnitAbility=true; }
		public void Init(int fID, int idx){ facID=fID; index=idx; isFacAbility=true; }
		
		[HideInInspector] public bool isUnitAbility=false;
		[HideInInspector] public bool isFacAbility=false;
		
		public _AbilityType type;
		public bool IsLine(){ return type==_AbilityType.Line; }
		//public bool IsGeneric(){ return type==_AbilityType.Generic; }
		//public bool IsTeleport(){ return type==_AbilityType.Teleport; }
		
		public _TargetType targetType;
		public bool requireTarget=true;
		public bool requireLos=true;
		public _SkillRangeType skillRangeType=_SkillRangeType.Distance;
		public int rangeMin=0;
		public int range=5;
		public int aoeRange=0;
		public bool useLineAOE=false;	//aoe only applies in the directions of the adjacent neighbours

		[Header("Multiple Target Lock")]
		public bool multipleTargetLock=false;	//Unit ability only: automatically lock hostile units instead of selecting one target
		public bool multipleTargetLockUseSight=true;	//When true, use srcUnit.GetSight() as the lock range. When false, use ability range
		public bool multipleTargetLockRequireLOS=true;	//When true, each locked hostile unit must pass line-of-sight check
		public int multipleTargetLockMaxTargets=0;	//0 means unlimited targets
		public float multipleTargetLockShootDelay=0.08f;	//Delay between locked target shots when visual shoot objects are fired
		
		public int fov=60;
		
		//[HideInInspector]
		public bool TargetStraightLineOnly(){ return type==_AbilityType.Charge | type==_AbilityType.Line; }
		public bool TargetCone(){ return type==_AbilityType.Cone; }
		
		public Unit spawnUnitPrefab;
		public Unit[] requiredUnit=new Unit[0];
		public int fusionRange=2;
		public bool fusionUseMainNode=true;
		public bool changeFormKeepHPPercent=true;
		public GameObject obstaclePrefab;
		
		public int moveCost;
		public int attackCost;
		public int abilityCost;
		public int apCost;
		public bool endAllActionAfterUse;
		
		public int cooldown=1;
		[HideInInspector] private int currentCD;	//runtime attribute
		
		public int useLimit=0;
		[HideInInspector] private int useCount;	//runtime attribute
		
		public float impactDelay=0f;
		
		
		
		//for generic type
		public _ImpactType impactType;
		
		public bool HasNoImpact(){ return impactType==_ImpactType.None; }
		public bool HasPositiveImpact(){ return impactType==_ImpactType.Positive; }
		public bool HasNegativeImpact(){ return impactType==_ImpactType.Negative; }
		
		public int hpModifierMin=5;
		public int hpModifierMax=5;
		public int GetRandHPModifier(){ return (int)Mathf.Round(Rand.Range(GetHPMin(), GetHPMax())); }
		
		public int apModifierMin=5;
		public int apModifierMax=5;
		public int GetRandAPModifier(){ return (int)Mathf.Round(Rand.Range(GetAPMin(), GetAPMax())); }
		
		//used when impactType=_AbInstantImpact.Negative
		public int damageType=0;					//public bool useDamageTable=false;
		public float attack=1;							//public float GetAttack(){ return attack; }
		public float hitChance=1;						//public float GetHit(){ return hitChance; }
		public float critChance=0;					//public float GetCritChance(){ return critChance; }
		public float critMultiplier=2;					//public float GetCritMultiplier(){ return critMultiplier; }
		public bool factorInTargetStats=false;	
		
		
		public float effHitChance=0f;	//applies for effctIDList,  clearAllEffect, switchFaction
		
		public List<int> effectIDList=new List<int>();
		
		public bool clearAllEffect=false;
		
		//for factionSwitch
		public bool switchFaction=false;
		//public int switchFacDuration=1;	
		public bool switchFacControllable=true;	//can be unit be controlled directly after a faction-switch
		
		//public int revealFogDuration=1;
		
		public int duration=1;	//for switch-faction and reveal-fog
		
		//animation
		public bool useAttackSequence;
		public bool fireShootObjectWithAbilityAnimation=false;	//When Use Attack Sequence is false, still fire ShootObject after AbilityX animation
		public bool aimAtUnit;
		public ShootObject shootObject;

		[Header("Ability Animation")]
		public string abilityAnimationTrigger="Ability1";	//Used when Use Attack Sequence is false. Example: Ability1, Ability2, Ability3.
		public string abilityAnimationState="Ability1";		//Animator state name to wait for before JRPG melee unit returns. Usually same as trigger.
		public int abilityAnimationLayer=0;
		public bool waitForAbilityAnimationComplete=true;	//JRPG mode: wait until Ability1/Ability2/etc animation completes before returning.
		public float abilityAnimationTimeout=8f;		//Safety timeout so a looping/missing animation does not lock the game forever.

		public float jrpgMeleeStepDistance=2f;	//JRPG mode only: visual step distance from target, measured in node-size units, for melee ability

		[Header("Ability Action Cam Timeline")]
		public bool useActionCamTimeline=false;
		public AbilityActionCamStartMode actionCamTimelineStartMode=AbilityActionCamStartMode.OnAbilityAnimationStart;
		public bool actionCamTimelineReturnToNormal=true;
		public float actionCamTimelineHoldAfterLast=0.25f;
		public float actionCamTimelineReturnDuration=0.5f;
		public List<AbilityActionCamKeyframe> actionCamTimeline=new List<AbilityActionCamKeyframe>();

		
		
		//visual effects
		public VisualObject effectOnUse;
		public VisualObject effectOnHit;
		
		public AudioClip activateSound;
		
		
		public enum _AbilityStatus{
			Ready,
			OnCooldown,
			HitUsedLimit,
			Disabled,
			InsufficientAP,
			HitAbilityPerTurnLimit,
			HitMovePerTurnLimit,
		}
		
		public _AbilityStatus IsAvailable(){
			if(currentCD>0) return _AbilityStatus.OnCooldown;
			if(HasUseLimit() && GetUseRemain()<=0) return _AbilityStatus.HitUsedLimit;
			
			if(isUnitAbility){
				if(srcUnit.AbilityDisabled()) return _AbilityStatus.Disabled;
				if(srcUnit.ap<GetAPCost()) return _AbilityStatus.InsufficientAP;
				if(srcUnit.GetAbilityRemain()<abilityCost) return _AbilityStatus.HitAbilityPerTurnLimit;
				if(srcUnit.GetMoveRemain()<moveCost || srcUnit.GetAttackRemain()<attackCost) return _AbilityStatus.HitMovePerTurnLimit;
			}
			
			//check available target?
			return _AbilityStatus.Ready;
		}
		
		public void IterateCD(){ if(currentCD>0) currentCD-=1; }
		public int GetCurrentCD(){ return currentCD-1; }
		
		public bool HasUseLimit(){ return useLimit>0; }
		public int GetUseRemain(){ return GetUseLimit()-useCount; }
		
		public void Activate(){
			Debug.Log("Ability activated - "+name);
			
			useCount+=1;
			currentCD=GetCooldown();
			
			if(srcUnit!=null){
				// Always pay the ability AP cost first, even when the ability ends all actions.
				// Previously AP was only deducted when endAllActionAfterUse was false.
				int actualAPCost=GetAPCost();
				if(actualAPCost>0){
					srcUnit.ap=Mathf.Max(0, srcUnit.ap-actualAPCost);
				}
				
				if(!endAllActionAfterUse){
					srcUnit.moveThisTurn+=moveCost;	
					srcUnit.attackThisTurn+=attackCost;	
					srcUnit.abilityThisTurn+=abilityCost;
				}
				else{
					srcUnit.EndAllAction();
				}
				
				effectOnUse.Spawn(srcUnit.GetPos());
			}
			
			Debug.Log("Play activateSound - "+activateSound);
			AudioManager.PlaySound(activateSound);
		}

		
		
		

		private float GetUnitHPRatio(Unit unit){
			if(unit==null || unit.GetFullHP()<=0) return 1f;
			return Mathf.Clamp01((float)unit.hp/(float)unit.GetFullHP());
		}

		private void ApplyHPRatio(Unit unit, float hpRatio){
			if(unit==null) return;
			int hp=Mathf.Max(1, Mathf.RoundToInt(unit.GetFullHP()*Mathf.Clamp01(hpRatio)));
			unit.hp = hp;
		}

		private bool HasOptionalRequiredFriendlyUnits(Unit sourceUnit){
			if(sourceUnit==null) return false;
			if(requiredUnit==null || requiredUnit.Length==0) return true;

			List<Unit> friendly=UnitManager.GetAllFriendlyUnits(sourceUnit.GetFacID());
			if(friendly==null) return false;

			for(int i=0; i<requiredUnit.Length; i++){
				Unit req=requiredUnit[i];
				if(req==null) continue;

				bool found=false;
				for(int n=0; n<friendly.Count; n++){
					Unit cand=friendly[n];
					if(cand==null || cand==sourceUnit) continue;
					if(cand.prefabID!=req.prefabID) continue;
					found=true;
					break;
				}

				if(!found) return false;
			}

			return true;
		}

		private List<Unit> FindFusionPartners(Unit sourceUnit){
			List<Unit> partners=new List<Unit>();

			if(sourceUnit==null){
				Debug.LogWarning("Fusion failed: source unit is null");
				return partners;
			}

			if(requiredUnit==null || requiredUnit.Length==0){
				Debug.LogWarning("Fusion failed: required unit list is empty");
				return partners;
			}

			List<Unit> friendly=UnitManager.GetAllFriendlyUnits(sourceUnit.GetFacID());
			if(friendly==null){
				Debug.LogWarning("Fusion failed: friendly unit list is null");
				return partners;
			}

			for(int i=0; i<requiredUnit.Length; i++){
				Unit req=requiredUnit[i];
				if(req==null) continue;

				Unit found=null;
				for(int n=0; n<friendly.Count; n++){
					Unit cand=friendly[n];
					if(cand==null || cand==sourceUnit) continue;
					if(partners.Contains(cand)) continue;
					if(cand.prefabID!=req.prefabID) continue;
					if(cand.node==null || sourceUnit.node==null) continue;

					if(GridManager.GetDistance(sourceUnit.node, cand.node)>fusionRange) continue;

					found=cand;
					break;
				}

				if(found==null){
					Debug.LogWarning("Fusion failed: missing required friendly unit within range for prefabID="+req.prefabID);
					partners.Clear();
					return partners;
				}

				partners.Add(found);
			}

			return partners;
		}

		private void SafeRemoveBattleUnit(Unit unit){
			if(unit==null) return;

			if(unit.node!=null){
				unit.node.unit=null;
				unit.node=null;
			}

			List<Faction> factions=UnitManager.GetFactionList();
			if(factions!=null){
				for(int i=0; i<factions.Count; i++){
					if(factions[i]==null || factions[i].unitList==null) continue;
					factions[i].unitList.Remove(unit);
				}
			}

			List<Unit> allUnits=UnitManager.GetAllUnitList();
			if(allUnits!=null) allUnits.Remove(unit);

			TBTK.OnUnitDestroyed(unit);
			UnityEngine.Object.Destroy(unit.gameObject);
		}

		private void AddReplacementUnitDirect(Unit unit, int facID){
			if(unit==null) return;

			List<Faction> factions=UnitManager.GetFactionList();
			if(factions!=null){
				for(int i=0; i<factions.Count; i++){
					if(factions[i]==null) continue;
					if(factions[i].factionID!=facID) continue;

					unit.SetFacID(facID);
					unit.playableUnit=factions[i].playableFaction;
					unit.NewTurn();

					if(!factions[i].unitList.Contains(unit)) factions[i].unitList.Add(unit);

					TBTK.OnNewUnit(unit, i);
					break;
				}
			}

			if(TurnControl.IsUnitPerTurn()){
				List<Unit> allUnits=UnitManager.GetAllUnitList();
				if(allUnits!=null && !allUnits.Contains(unit)) allUnits.Add(unit);
			}

			TBTK.OnUnitOrderChanged();
		}

		private Unit SpawnReplacementUnit(Unit prefab, Node spawnNode, int facID, bool playable, Quaternion rot, float hpRatio){
			if(prefab==null || spawnNode==null){
				Debug.LogWarning("Replacement failed: prefab or spawnNode is null");
				return null;
			}

			GameObject obj=(GameObject)UnityEngine.Object.Instantiate(prefab.gameObject, spawnNode.GetPos(), rot);
			Unit newUnit=obj.GetComponent<Unit>();
			if(newUnit==null){
				Debug.LogWarning("Replacement failed: spawned prefab has no Unit component");
				UnityEngine.Object.Destroy(obj);
				return null;
			}

			newUnit.SetFacID(facID);
			newUnit.playableUnit=playable;
			newUnit.node=spawnNode;
			spawnNode.unit=newUnit;
			ApplyHPRatio(newUnit, hpRatio);
			AddReplacementUnitDirect(newUnit, facID);

			return newUnit;
		}

		private bool DoChangeFormCAS(Node targetNode){
			Debug.Log("ChangeForm/CAS attempt");

			if(srcUnit==null){
				Debug.LogWarning("ChangeForm/CAS failed: srcUnit is null");
				return false;
			}

			if(spawnUnitPrefab==null){
				Debug.LogWarning("ChangeForm/CAS failed: target form unit is not assigned");
				return false;
			}

			if(srcUnit.node==null){
				Debug.LogWarning("ChangeForm/CAS failed: source unit has no node");
				return false;
			}

			if(!HasOptionalRequiredFriendlyUnits(srcUnit)){
				Debug.LogWarning("ChangeForm/CAS failed: required carrier/friendly unit is missing");
				return false;
			}

			Node spawnNode=srcUnit.node;
			int sourceFacID=srcUnit.GetFacID();
			bool wasSelected=UnitManager.GetSelectedUnit()==srcUnit;
			Quaternion rot=srcUnit.transform.rotation;
			float hpRatio=changeFormKeepHPPercent ? GetUnitHPRatio(srcUnit) : 1f;

			SafeRemoveBattleUnit(srcUnit);

			Unit newUnit=SpawnReplacementUnit(spawnUnitPrefab, spawnNode, sourceFacID, true, rot, hpRatio);
			if(newUnit==null) return false;

			if(wasSelected){
				UnitManager.TBSelectUnit(newUnit);
				TBTK.OnSelectUnit(newUnit);
			}

			GridManager.SetupFogOfWar();
			Debug.Log("ChangeForm/CAS complete: "+newUnit.gameObject.name);
			return true;
		}

		private bool DoFusion(Node targetNode){
			Debug.Log("Fusion attempt");

			if(srcUnit==null){
				Debug.LogWarning("Fusion failed: srcUnit is null");
				return false;
			}

			if(spawnUnitPrefab==null){
				Debug.LogWarning("Fusion failed: fused unit prefab is not assigned");
				return false;
			}

			if(srcUnit.node==null){
				Debug.LogWarning("Fusion failed: source unit has no node");
				return false;
			}

			List<Unit> partners=FindFusionPartners(srcUnit);
			if(partners==null || partners.Count==0) return false;

			Node spawnNode=fusionUseMainNode ? srcUnit.node : targetNode;
			if(spawnNode==null) spawnNode=srcUnit.node;
			if(spawnNode==null){
				Debug.LogWarning("Fusion failed: spawn node is null");
				return false;
			}

			int sourceFacID=srcUnit.GetFacID();
			bool wasSelected=UnitManager.GetSelectedUnit()==srcUnit;
			Quaternion rot=srcUnit.transform.rotation;
			float hpRatio=GetUnitHPRatio(srcUnit);

			for(int i=0; i<partners.Count; i++){
				SafeRemoveBattleUnit(partners[i]);
			}

			SafeRemoveBattleUnit(srcUnit);

			Unit fusedUnit=SpawnReplacementUnit(spawnUnitPrefab, spawnNode, sourceFacID, true, rot, hpRatio);
			if(fusedUnit==null) return false;

			if(wasSelected){
				UnitManager.TBSelectUnit(fusedUnit);
				TBTK.OnSelectUnit(fusedUnit);
			}

			GridManager.SetupFogOfWar();
			Debug.Log("Fusion complete: "+fusedUnit.gameObject.name);
			return true;
		}

		public IEnumerator HitTarget(Node node){
			Debug.Log("HitTarget "+type);
			
			if(impactDelay>0) yield return new WaitForSeconds(impactDelay);
			
			Debug.Log("HitTarget "+type+"   "+obstaclePrefab);
			
			if(type==_AbilityType.Generic){
				//Debug.Log(!isUnitAbility +"   "+ !requireTarget);
				if(!isUnitAbility && !requireTarget){	//faction ability that doesn't require target will get all valid target on the grid
					List<Node> nodeList=GridManager.GetTargetNodeForNonTargetingFAbility(this);
					//Debug.Log("  "+nodeList.Count);
					for(int i=0; i<nodeList.Count; i++){
						
						nodeList[i].unit.ApplyAttack(this);
						effectOnHit.Spawn(nodeList[i].GetPos());
					}
				}
				else{
					int aoe=GetAOE();
					if(aoe<=0){
						if(node.unit!=null) node.unit.ApplyAttack(this);
					}
					else{
						List<Node> nodeList=GridManager.GetNodesWithinDistance(node, aoe);
						nodeList.Add(node);
						
						for(int i=0; i<nodeList.Count; i++){
							if(nodeList[i].unit==null) continue; 
							
							if(targetType==Ability._TargetType.AllUnit){
								nodeList[i].unit.ApplyAttack(this);
							}
							else if(targetType==Ability._TargetType.HostileUnit){
								if(nodeList[i].unit.GetFacID()!=facID) nodeList[i].unit.ApplyAttack(this);
							}
							else if(targetType==Ability._TargetType.FriendlyUnit){
								if(nodeList[i].unit.GetFacID()==facID) nodeList[i].unit.ApplyAttack(this);
							}
						}
					}
				}
			}
			else if(type==_AbilityType.Teleport){
				if(srcUnit!=null){
					srcUnit.node.unit=null;
					srcUnit.node=node;
					srcUnit.node.unit=srcUnit;
					srcUnit.GetT().position=node.GetPos();
					
					GridManager.SetupFogOfWar();
					UnitManager.CheckAITrigger(srcUnit);
					if(node.collectible!=null) yield return srcUnit.StartCoroutine(node.collectible.Trigger(srcUnit));
				}
			}
			else if(type==_AbilityType.SpawnUnit){
				if(spawnUnitPrefab!=null){
					GameObject obj=(GameObject)MonoBehaviour.Instantiate(spawnUnitPrefab.gameObject, node.GetPos(), Quaternion.identity);
					Unit unit=obj.GetComponent<Unit>();
					unit.node=node;	node.unit=unit;
					unit.hp = unit.GetFullHP();
					UnitManager.AddUnit(unit, srcUnit!=null ? srcUnit.GetFacID() : facID);
					
					GridManager.SetupFogOfWar();
				}
				else Debug.Log("No unit prefab has been assigned!!");
			}
			else if(type==_AbilityType.ChangeForm){
				DoChangeFormCAS(node);
			}
			else if(type==_AbilityType.Fusion){
				DoFusion(node);
			}
			else if(type==_AbilityType.Charge){
				if(node.unit!=null){
					float wantedAngle=GridManager.GetAngle(srcUnit.node, node, true);	
					float tgtDist=GridManager.GetDistance(srcUnit.node, node);
					List<Node> neighbours=node.GetNeighbourList(true);
					for(int i=0; i<neighbours.Count; i++){
						float dist=GridManager.GetDistance(srcUnit.node, neighbours[i]);
						float angle=Mathf.Abs(wantedAngle-GridManager.GetAngle(srcUnit.node, neighbours[i], true));
						if(dist>=tgtDist || angle>1) continue;
						yield return CRoutine.Get().StartCoroutine(srcUnit.MoveRoutine(neighbours[i], 3));
						break;
					}
				}
				
				int aoe=GetAOE();
				if(aoe<=0){
					if(node.unit!=null) node.unit.ApplyAttack(this);
				}
				else{
					List<Node> nodeList=GridManager.GetNodesWithinDistance(node, aoe);
					for(int i=0; i<nodeList.Count; i++){
						if(nodeList[i].unit!=null && !OnSameFac(srcUnit, nodeList[i].unit)) nodeList[i].unit.ApplyAttack(this);
					}
				}
			}
			else if(type==_AbilityType.Line || type==_AbilityType.Cone){
				List<Node> nodeList=new List<Node>();
				if(type==_AbilityType.Line) nodeList=GridManager.GetNodesInALine(srcUnit.node, GridManager.GetAngle(node, srcUnit.node, true), GetRange(), GetRangeMin());
				if(type==_AbilityType.Cone) nodeList=GridManager.GetNodesInACone(srcUnit.node, node, GetRange(), GetRangeMin(), fov);
				
				for(int i=0; i<nodeList.Count; i++){
					if(nodeList[i].unit==null) continue;
					
					if(targetType==_TargetType.HostileUnit){
						if(!OnSameFac(srcUnit, nodeList[i].unit)) nodeList[i].unit.ApplyAttack(this);
					}
					else if(targetType==_TargetType.FriendlyUnit){
						if(OnSameFac(srcUnit, nodeList[i].unit)) nodeList[i].unit.ApplyAttack(this);
					}
					else nodeList[i].unit.ApplyAttack(this);
				}
			}
			else if(type==_AbilityType.ScanFogOfWar){
				if(GameControl.EnableFogOfWar()){
					List<Node> nodeList=GridManager.GetNodesWithinDistance(node, GetAOE());
					for(int i=0; i<nodeList.Count; i++) nodeList[i].RevealFogOfWar(GetDuration());
				}
			}
			else if(type==_AbilityType.DeployBlock){
				if(obstaclePrefab!=null){
					node.obstacleT=(Transform)MonoBehaviour.Instantiate(obstaclePrefab.transform, node.GetPos(), Quaternion.identity);
					
					List<Node> neighbours=node.GetNeighbourList();
					for(int i=0; i<neighbours.Count; i++){
						if(!neighbours[i].walkable || neighbours[i].HasObstacle()) continue;
						neighbours[i].InitCover();
					}
					
					GridManager.SetupFogOfWar();
				}
				else Debug.Log("No obstacle prefab has been assigned!!");
			}
			
			if(node!=null) effectOnHit.Spawn(node.GetPos());
		}

		public IEnumerator HitMultipleTargetLock(List<Unit> targetList){
			Debug.Log("HitMultipleTargetLock "+name+" targets="+(targetList!=null ? targetList.Count : 0));
			
			if(impactDelay>0) yield return new WaitForSeconds(impactDelay);
			if(targetList==null) yield break;
			
			for(int i=0; i<targetList.Count; i++){
				Unit target=targetList[i];
				if(target==null || target.node==null || target.hp<=0) continue;
				if(srcUnit!=null && target.GetFacID()==srcUnit.GetFacID()) continue;
				
				// Multiple Target Lock is hostile-only. Generic abilities apply normal ability impact/effects.
				// None abilities are allowed for custom/no-impact visual lock-on abilities.
				if(type==_AbilityType.Generic) target.ApplyAttack(this);
				
				effectOnHit.Spawn(target.GetPos());
			}
		}

		public bool IsMultipleTargetLock(){ return isUnitAbility && multipleTargetLock; }
		public int GetMultipleTargetLockRange(){
			if(multipleTargetLockUseSight && srcUnit!=null) return srcUnit.GetSight();
			return GetRange();
		}
		public int GetMultipleTargetLockMaxTargets(){ return Mathf.Max(0, multipleTargetLockMaxTargets); }
		public float GetMultipleTargetLockShootDelay(){ return Mathf.Max(0, multipleTargetLockShootDelay); }

		public bool UseActionCamTimeline(){ return isUnitAbility && useActionCamTimeline && actionCamTimeline!=null && actionCamTimeline.Count>0; }
		public bool StartActionCamTimelineOnActivation(){ return actionCamTimelineStartMode==AbilityActionCamStartMode.OnAbilityActivated; }
		public bool StartActionCamTimelineOnAnimationStart(){ return actionCamTimelineStartMode==AbilityActionCamStartMode.OnAbilityAnimationStart; }
		public float GetActionCamTimelineHoldAfterLast(){ return Mathf.Max(0, actionCamTimelineHoldAfterLast); }
		public float GetActionCamTimelineReturnDuration(){ return Mathf.Max(0, actionCamTimelineReturnDuration); }
		
		public static bool OnSameFac(Unit unit1, Unit unit2){ return unit1.GetFacID()==unit2.GetFacID(); }
		
		
		public bool IsUAB(){ return isUnitAbility; }
		
		
		public List<int> GetRuntimeEffectIDList(){
			if(IsUAB())	return PerkManager.ModifyUAbilityEffectList(prefabID, new List<int>( effectIDList ));
			else			return PerkManager.ModifyFAbilityEffectList(prefabID, new List<int>( effectIDList ));
		}
		
		public int GetAPCost(){ 
			if(IsUAB())	return (int)(apCost * PerkManager.GetUAbilityMulAPCost(prefabID) + PerkManager.GetUAbilityModAPCost(prefabID));
			else 			return (int)(apCost * PerkManager.GetFAbilityMulAPCost(prefabID) + PerkManager.GetFAbilityModAPCost(prefabID));
		}
		public int GetDuration(){ 
			if(IsUAB()) return (int)(duration * PerkManager.GetUAbilityMulDur(prefabID) + PerkManager.GetUAbilityModDur(prefabID));
			else 			return(int)( duration * PerkManager.GetFAbilityMulDur(prefabID) + PerkManager.GetFAbilityModDur(prefabID));
		}
		public int GetCooldown(){ 
			if(IsUAB()) return (int)(cooldown * PerkManager.GetUAbilityMulCD(prefabID) + PerkManager.GetUAbilityModCD(prefabID));
			else 			return(int)( cooldown * PerkManager.GetFAbilityMulCD(prefabID) + PerkManager.GetFAbilityModCD(prefabID));
		}
		public int GetUseLimit(){ 
			if(IsUAB()) return (int)(useLimit * PerkManager.GetUAbilityMulUseLim(prefabID) + PerkManager.GetUAbilityModUseLim(prefabID));
			else 			return (int)(useLimit * PerkManager.GetFAbilityMulUseLim(prefabID) + PerkManager.GetFAbilityModUseLim(prefabID));
		}
		
		
		public float GetAttack(){
			if(IsUAB()) return attack * PerkManager.GetUAbilityMulAttack(prefabID) + PerkManager.GetUAbilityModAttack(prefabID);
			else 			return attack * PerkManager.GetFAbilityMulAttack(prefabID) + PerkManager.GetFAbilityModAttack(prefabID);
		}
		public float GetHit(){
			if(IsUAB()) return hitChance * PerkManager.GetUAbilityMulHit(prefabID) + PerkManager.GetUAbilityModHit(prefabID);
			else 			return hitChance * PerkManager.GetFAbilityMulHit(prefabID) + PerkManager.GetFAbilityModHit(prefabID);
		}
		public float GetHPMin(){
			if(IsUAB()) return hpModifierMin * PerkManager.GetUAbilityMulDmgHPMin(prefabID) + PerkManager.GetUAbilityModDmgHPMin(prefabID);
			else 			return hpModifierMin * PerkManager.GetFAbilityMulDmgHPMin(prefabID) + PerkManager.GetFAbilityModDmgHPMin(prefabID);
		}
		public float GetHPMax(){
			if(IsUAB()) return hpModifierMax * PerkManager.GetUAbilityMulDmgHPMax(prefabID) + PerkManager.GetUAbilityModDmgHPMax(prefabID);
			else 			return hpModifierMax * PerkManager.GetFAbilityMulDmgHPMax(prefabID) + PerkManager.GetFAbilityModDmgHPMax(prefabID);
		}
		public float GetAPMin(){
			if(IsUAB()) return apModifierMin * PerkManager.GetUAbilityMulDmgAPMin(prefabID) + PerkManager.GetUAbilityModDmgAPMin(prefabID);
			else 			return apModifierMin * PerkManager.GetFAbilityMulDmgAPMin(prefabID) + PerkManager.GetFAbilityModDmgAPMin(prefabID);
		}
		public float GetAPMax(){
			if(IsUAB()) return apModifierMax * PerkManager.GetUAbilityMulDmgAPMax(prefabID) + PerkManager.GetUAbilityModDmgAPMax(prefabID);
			else 			return apModifierMax * PerkManager.GetFAbilityMulDmgAPMax(prefabID) + PerkManager.GetFAbilityModDmgAPMax(prefabID);
		}
		public float GetCritChance(){
			if(IsUAB()) return critChance * PerkManager.GetUAbilityMulCritC(prefabID) + PerkManager.GetUAbilityModCritC(prefabID);
			else 			return critChance * PerkManager.GetFAbilityMulCritC(prefabID) + PerkManager.GetFAbilityModCritC(prefabID);
		}
		public float GetCritMultiplier(){
			if(IsUAB()) return critMultiplier * PerkManager.GetUAbilityMulCritM(prefabID) + PerkManager.GetUAbilityModCritM(prefabID);
			else 			return critMultiplier * PerkManager.GetFAbilityMulCritM(prefabID) + PerkManager.GetFAbilityModCritM(prefabID);
		}
		
		public bool IsMeleeSkill(){ return skillRangeType==_SkillRangeType.Melee; }

		public int GetRangeMin(){
			// Both Distance and Melee ability use the ability's own range values.
			// SkillRangeType only changes activation/animation behaviour, not where the range is read from.
			if(IsUAB()) return (int)(rangeMin * PerkManager.GetUAbilityMulRange(prefabID) + PerkManager.GetUAbilityModRange(prefabID));
			else 			return rangeMin;
		}
		public int GetRange(){
			// Both Distance and Melee ability use the ability's own range values.
			// This prevents melee ability from being forced to Unit.statsMelee attack range.
			if(IsUAB()) return (int)(range * PerkManager.GetUAbilityMulRange(prefabID) + PerkManager.GetUAbilityModRange(prefabID));
			else 			return range;
		}
		public int GetAOE(){
			if(IsUAB()) return (int)(aoeRange * PerkManager.GetUAbilityMulAOE(prefabID) + PerkManager.GetUAbilityModAOE(prefabID));
			else 			return (int)(aoeRange * PerkManager.GetFAbilityMulAOE(prefabID) + PerkManager.GetFAbilityModAOE(prefabID));
		}
		
		public float GetEffHitChance(){
			if(IsUAB()) return (int)(effHitChance * PerkManager.GetUAbilityMulEffHitC(prefabID) + PerkManager.GetUAbilityModEffHitC(prefabID));
			else 			return (int)(effHitChance * PerkManager.GetFAbilityMulEffHitC(prefabID) + PerkManager.GetFAbilityModEffHitC(prefabID));
		}
		
		
		
		public Ability Clone(){
			Ability clone=new Ability();
			
			base.Clone(this, clone);
			
			clone.type=type;
			
			clone.targetType=targetType;
			clone.requireTarget=requireTarget;		clone.requireLos=requireLos;
			clone.skillRangeType=skillRangeType;
			clone.rangeMin=rangeMin;					clone.range=range;							clone.aoeRange=aoeRange;		
			clone.fov=fov;

			clone.multipleTargetLock=multipleTargetLock;
			clone.multipleTargetLockUseSight=multipleTargetLockUseSight;
			clone.multipleTargetLockRequireLOS=multipleTargetLockRequireLOS;
			clone.multipleTargetLockMaxTargets=multipleTargetLockMaxTargets;
			clone.multipleTargetLockShootDelay=multipleTargetLockShootDelay;
			
			
			clone.spawnUnitPrefab=spawnUnitPrefab;
			clone.requiredUnit=requiredUnit!=null ? (Unit[])requiredUnit.Clone() : new Unit[0];
			clone.fusionRange=fusionRange;
			clone.fusionUseMainNode=fusionUseMainNode;
			clone.changeFormKeepHPPercent=changeFormKeepHPPercent;
			clone.obstaclePrefab=obstaclePrefab;
			
			clone.moveCost=moveCost;				clone.attackCost=attackCost;			clone.abilityCost=abilityCost;
			clone.apCost=apCost;						clone.endAllActionAfterUse=endAllActionAfterUse;
			
			clone.duration=duration;					clone.cooldown=cooldown;				clone.useLimit=useLimit;
			clone.impactDelay=impactDelay;
			
			clone.impactType=impactType;
			clone.hpModifierMin=hpModifierMin;		clone.hpModifierMax=hpModifierMax;
			clone.apModifierMin=apModifierMin;		clone.apModifierMax=apModifierMax;
			
			clone.damageType=damageType;
			clone.attack=attack;						clone.hitChance=hitChance;
			clone.critChance=critChance;			clone.critMultiplier=critMultiplier;
			clone.factorInTargetStats=factorInTargetStats;
			
			clone.effHitChance=effHitChance;
			
			clone.effectIDList=new List<int>( effectIDList );
			clone.clearAllEffect=clearAllEffect;
			
			clone.switchFaction=switchFaction;
			//clone.switchFacDuration=switchFacDuration;
			clone.switchFacControllable=switchFacControllable;
			
			clone.useAttackSequence=useAttackSequence;
			clone.fireShootObjectWithAbilityAnimation=fireShootObjectWithAbilityAnimation;
			clone.aimAtUnit=aimAtUnit;
			clone.shootObject=shootObject;
			clone.abilityAnimationTrigger=abilityAnimationTrigger;
			clone.abilityAnimationState=abilityAnimationState;
			clone.abilityAnimationLayer=abilityAnimationLayer;
			clone.waitForAbilityAnimationComplete=waitForAbilityAnimationComplete;
			clone.abilityAnimationTimeout=abilityAnimationTimeout;
			clone.jrpgMeleeStepDistance=jrpgMeleeStepDistance;
			
			clone.useActionCamTimeline=useActionCamTimeline;
			clone.actionCamTimelineStartMode=actionCamTimelineStartMode;
			clone.actionCamTimelineReturnToNormal=actionCamTimelineReturnToNormal;
			clone.actionCamTimelineHoldAfterLast=actionCamTimelineHoldAfterLast;
			clone.actionCamTimelineReturnDuration=actionCamTimelineReturnDuration;
			clone.actionCamTimeline=new List<AbilityActionCamKeyframe>();
			if(actionCamTimeline!=null){
				for(int i=0; i<actionCamTimeline.Count; i++){
					AbilityActionCamKeyframe srcFrame=actionCamTimeline[i];
					if(srcFrame==null) continue;
					AbilityActionCamKeyframe newFrame=new AbilityActionCamKeyframe();
					newFrame.anchor=srcFrame.anchor;
					newFrame.position=srcFrame.position;
					newFrame.rotation=srcFrame.rotation;
					newFrame.timeStamp=srcFrame.timeStamp;
					newFrame.chain=srcFrame.chain;
					clone.actionCamTimeline.Add(newFrame);
				}
			}
			
			clone.effectOnUse=effectOnUse!=null ? effectOnUse.Clone() : new VisualObject();
			clone.effectOnHit=effectOnHit!=null ? effectOnHit.Clone() : new VisualObject();
			
			clone.activateSound=activateSound;
			
			return clone;
		}
	}

}