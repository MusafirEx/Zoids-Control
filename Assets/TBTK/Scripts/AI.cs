using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TBTK{
	
	public class AI : MonoBehaviour {
		
		#if UNITY_EDITOR
		public static bool inspector=false;
		#endif
		
		
		public enum _AIBehaviour { passive, aggressive, }// evasive }
		
		
		[Tooltip("Check to override individual unit behaviour setting")]
		public bool overrideUnitSetting=false;
		
		public _AIBehaviour aiBehaviour=_AIBehaviour.aggressive;
		public bool requireTrigger=true;	//when true, unit starts in passive state, then switch to aggressive or evasive (doesnt apply for passive)
		
		public static bool IsPassive(Unit unit){
			if(!instance.overrideUnitSetting) return unit.IsPassive();
			else return instance.aiBehaviour==AI._AIBehaviour.passive;
		}
		public static bool IsAggressive(Unit unit){ 
			if(!instance.overrideUnitSetting) return unit.IsAggressive();
			else return (instance.aiBehaviour==AI._AIBehaviour.aggressive && !instance.requireTrigger) || unit.triggered;
		}
		
		//public static bool IsPassive(Unit unit){
			//if(!instance.overrideUnitSetting) return unit.IsPassive();
			//else return instance.aiBehaviour==AI._AIBehaviour.passive || !IsAggressive(unit);
		//}
		//public static bool IsAggressive(Unit unit){ 
			//if(!instance.overrideUnitSetting) return unit.IsPassive();
			//else return instance.aiBehaviour==AI._AIBehaviour.aggressive && (unit.triggered || !instance.requireTrigger);
		//}
		
		
		[Tooltip("Delay in second between each units when they take their turn")]
		public float delayBetweenUnit=0.25f;
		
		[Space(10)] [Tooltip("When checked, AI will always use the best option available")]
		public bool alwaysUseBestOption=true;
		
		[Space(10)]
		[Tooltip("How much the damage for a potential attack is going to be weight into making the AI decision ")]
		public float damageMultiplier=1;
		[Tooltip("How much the hit chance for a potential attack is going to be weight into making the AI decision ")]
		public float hitChanceMultiplier=1;
		[Tooltip("How much the critical chance for a potential attack is going to be weight into making the AI decision ")]
		public float critChanceMultiplier=1;
		[Tooltip("How much of giving chase on out of range target is going to be weight into making the AI decision\nOnly applies there are no target within range")]
		public float pursueMultiplier=1;
		[Tooltip("How much cover is going to be weight into making the AI decision\nOnly applies when cover system is enabled ")]
		public float coverMultiplier=1;

		[Space(10)]
		[Tooltip("Allow AI to use Unit Abilities, not only normal attacks")]
		public bool useAbilities=true;
		[Tooltip("Overall weight added to ability scores. Increase if AI rarely uses abilities.")]
		public float abilityMultiplier=1;
		[Tooltip("How much ability HP/AP impact affects AI decision.")]
		public float abilityImpactMultiplier=1;
		[Tooltip("How much AOE/multi-target ability value affects AI decision.")]
		public float abilityAOEMultiplier=1;
		[Tooltip("How much effect/status/special ability value affects AI decision.")]
		public float abilityEffectMultiplier=1;
		[Tooltip("Minimum score required before AI chooses an ability. Prevents wasting weak abilities.")]
		public float minimumAbilityScore=25;
		
		public static float dmgMul(){ return instance.damageMultiplier; }
		public static float hitMul(){ return instance.hitChanceMultiplier; }
		public static float critMul(){ return instance.critChanceMultiplier; }
		public static float pursueMul(){ return instance.pursueMultiplier; }
		public static float coverMul(){ return instance.coverMultiplier; }
		
		
		
		public static AI instance;
		public static void Init(){
			if(instance==null) instance=(AI)FindObjectOfType(typeof(AI));
		}
		
		void Awake(){ instance=this; actionInProgress=false; }
		
		
		[Space(10)] private static bool actionInProgress=false;
		public static bool ActionInProgress(){ return actionInProgress; }
		
		
		public static void MoveUnit(Unit unit){ instance.StartCoroutine(_MoveUnit(unit)); }
		public static IEnumerator _MoveUnit(Unit unit){
			actionInProgress=true;
			
			TBTK.OnGameMessage("- Enemy's Turn -");
			TBTK.OnSelectUnit(unit);
			
			yield return instance.StartCoroutine(AIRoutineUnit(unit));
			
			if(unit==null || unit.hp<=0) yield return new WaitForSeconds(0.25f);
			if(instance.delayBetweenUnit>0) yield return new WaitForSeconds(instance.delayBetweenUnit);
			
			actionInProgress=false;
			
			GameControl.EndTurn();
		}
		
		public static void MoveFaction(Faction faction){ instance.StartCoroutine(_MoveFaction(faction)); }
		public static IEnumerator _MoveFaction(Faction faction){
			actionInProgress=true;
			
			TBTK.OnGameMessage(" Enemy's Turn -");
			
			List<Unit> unitList=new List<Unit>( faction.unitList );
			
			if(TurnControl.EnableUnitLimit() || Rand.value()<0.4f){
				List<Unit> newList=new List<Unit>();
				while(unitList.Count>0){
					int rand=Rand.Range(0, unitList.Count);
					newList.Add(unitList[rand]);
					unitList.RemoveAt(rand);
				}
				unitList=newList;
			}
			
			if(unitList.Count>TurnControl.GetUnitLimit()){
				while(unitList.Count>TurnControl.GetUnitLimit()) unitList.RemoveAt(unitList.Count-1);
			}
			
			for(int i=0; i<unitList.Count; i++){
				if(unitList[i]==null) continue;
				
				Unit unit=unitList[i];					TBTK.OnSelectUnit(unit);
				yield return instance.StartCoroutine(AIRoutineUnit(unit));
				
				if(unit==null || (unit.hp<=0 && unit.IsVisible())){ yield return new WaitForSeconds(0.25f); }
				
				if(unit!=null && instance.delayBetweenUnit>0 && unit.IsVisible()) yield return new WaitForSeconds(instance.delayBetweenUnit);
			}
			
			actionInProgress=false;
			
			GameControl.EndTurn();
		}
		
		public static IEnumerator AIRoutineUnit(Unit unit){
			if(unit==null || unit.IsStunned()) yield break;
			
			int safetyCounter=0;
			while(unit!=null && unit.hp>0 && (unit.CanMove() || unit.CanAttack() || UnitHasUsableAbility(unit))){
				AIAction action=AnalyseAction(unit);
				
				if(action==null){
					//Debug.Log("No valid action for unit - "+unit.gameObject);
					yield break;
				}
				
				if(action.tgtNode==unit.node && action.tgtUnit==null && !action.IsAbilityAction()){
					safetyCounter+=1;
					if(safetyCounter>3) yield break;
					else continue;
				}
				else safetyCounter=0;
				
				if(action.tgtNode!=null && action.tgtNode!=unit.node){
					yield return instance.StartCoroutine(unit.MoveRoutine(action.tgtNode));
				}
				
				if(unit==null || unit.hp<=0) yield break;
				
					if(action.IsAbilityAction()){
						// Re-check after moving because movement can spend AP/move count.
						if(action.ability!=null && unit!=null && action.ability.IsAvailable()==Ability._AbilityStatus.Ready){
							Node abilityTarget=action.abilityTargetNode!=null ? action.abilityTargetNode : unit.node;

							// Line/Cone skills use the target node as a direction/area reference.
							// Make the AI face the actual useful enemy/friendly target before starting the ability animation.
							yield return instance.StartCoroutine(RotateUnitTowardAbilityLookTarget(unit, action));

							// Unit.UseAbilityRoutine() changes Line target to tgtNode.abLineParent.
							// For AI, force that parent to the real look target so the unit does not turn to a wrong/stale node.
							if(action.ability.type==Ability._AbilityType.Line && abilityTarget!=null && action.abilityLookNode!=null){
								abilityTarget.abLineParent=action.abilityLookNode;
							}

							yield return instance.StartCoroutine(unit.UseAbilityRoutine(action.ability, abilityTarget));

							// AI rule: after using one ability, this unit's AI turn ends immediately.
							// This also safely handles ChangeForm/CAS and Fusion, because those abilities replace/destroy the source Unit.
							yield break;
						}
					}
				else if(unit!=null && action.tgtUnit!=null){
					yield return instance.StartCoroutine(unit.AttackRoutine(action.tgtUnit.node));
				}
				
				yield return null;
			}
			
			yield return null;
		}
		
		public static AIAction AnalyseAction(Unit unit){
			//Debug.Log("AnalyseAction "+unit.gameObject);
			
			if(IsPassive(unit) && Rand.value()<0.7f) return new AIAction(unit.node);
			
			List<AIAction> actionList=new List<AIAction>();
			
			//List<Unit> hostileList=new List<Unit>();
			bool nearestHostileScanned=false;
			Node nearestHostileNode=null;	float nearestHostileDist=Mathf.Infinity;
			//Node furthestHostileNode=null;	float furthestHostileDist=0;
			float maxNearestDistToHostile=0;	float minNearestDistToHostile=Mathf.Infinity;
			bool hasTargetWithinRange=false;
			
			float unitDamage=-99;
			
			List<Node> walkableList=unit.CanMove() ? GridManager.SetupWalkableList(unit) : null;
			if(walkableList==null) walkableList=new List<Node>();
			walkableList.Insert(0, unit.node);
			
			List<Unit> allhostileList=null;
			if(GameControl.EnableCoverSystem()) allhostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
			
			
			for(int i=0; i<walkableList.Count; i++){
				List<Node> attackNodeList=GridManager.GetAttackableList(unit, walkableList[i]);
				
				Vector2 cover=CheckCover(walkableList[i], unit, allhostileList);
				float coverScore=100 * (cover[0]!=0 ? cover[0] : cover[1]) * 0.5f ; 	//bcz full cover value is 2
				
				if(attackNodeList.Count>0){
					if(unitDamage<=-99) unitDamage=Mathf.Max(1f, (unit.GetDmgHPMin() + unit.GetDmgHPMax()) * 0.5f);
					
					for(int n=0; n<attackNodeList.Count; n++){
						AIAction action=new AIAction(walkableList[i], attackNodeList[n].unit);
						
						Attack attack=new Attack(unit, attackNodeList[n].unit, walkableList[i], false, false);
						action.score+=(0.5f*(attack.damageHPMin+attack.damageHPMax))/unitDamage*100*instance.damageMultiplier;
						action.score+=attack.hitChance*100f*instance.hitChanceMultiplier;
						action.score+=attack.critChance*100f*instance.critChanceMultiplier;
						action.score+=coverScore;
						
						//~ float score1=(0.5f*(attack.damageHPMin+attack.damageHPMax))/unitDamage*100*instance.damageMultiplier;
						//~ float score2=attack.hitChance*100f*instance.hitChanceMultiplier;
						//~ float score3=attack.critChance*100f*instance.critChanceMultiplier;
						//~ float score4=coverScore;
						//~ float ccover=Attack.GetCover(walkableList[i], attackNodeList[n]);
						
						//~ Debug.Log((0.5f*(attack.damageHPMin+attack.damageHPMax))+"   "+unitDamage+"   "+attack.cover+"    "+score1+"   "+score2+"   "+score3+"   "+score4+"   ");
						
						//Debug.Log(coverScore+"   "+attack.cover+"   "+attack.hitChance+"    "+attack.critChance+"   "+action.score);
						//Debug.DrawLine(walkableList[i].GetPos(), walkableList[i].GetPos()+new Vector3(0, 1, 0)*(action.score*0.01f), Color.white, 2);
						
						actionList.Add(action);
					}
					
					hasTargetWithinRange=true;
				}
				else{
					
					AIAction action=new AIAction(walkableList[i], coverScore);
					
					if(IsPassive(unit)){
						action.score+=Mathf.Min(0, 3-GridManager.GetDistance(walkableList[i], unit.node));
					}
					else if(IsAggressive(unit)){
						//Node nearestHostileNode=null;	float nearestHostileDist=0;
						
						if(!nearestHostileScanned){
							nearestHostileScanned=true;
							
							if(allhostileList==null) allhostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
							
							if(allhostileList.Count>0){
								int nearestIdx=0;	//float nearest=Mathf.Infinity;
								//int furthestIdx=0;	//float furthest=0;
								for(int n=0; n<allhostileList.Count; n++){
									float dist=GridManager.GetDistance(unit.node, allhostileList[n].node);
									if(dist<nearestHostileDist){ nearestHostileDist=dist; nearestIdx=n; }
									//if(dist>furthest){ furthestHostileDist=dist; furthestIdx=n; }
								}
								nearestHostileNode=allhostileList[nearestIdx].node;
								//furthestHostileNode=hostileList[furthestIdx].node;
							}
						}
						
						//if(nearestHostileNode==null){
							//List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
							//~ int nearestIdx=0;	float nearest=Mathf.Infinity;
							//~ int furthestIdx=0;	float furthest=0;
							//~ for(int n=0; n<hostileList.Count; n++){
								//~ float dist=GridManager.GetDistance(unit.node, hostileList[n].node);
								//~ if(dist<nearest){ nearest=dist; nearestIdx=n; }
								//~ if(dist>furthest){ furthest=dist; furthestIdx=n; }
							//~ }
							//nearestHostileNode=hostileList[nearestIdx].node;
							//nearestHostileDist=nearest;
						//}
							
						if(nearestHostileNode!=null){
							action.scoreAlt=GridManager.GetDistance(walkableList[i], nearestHostileNode);
							//float nearestDistToHostile=GridManager.GetDistance(walkableList[i], nearestHostileNode);
							//action.scoreAlt=Mathf.Max(0, nearestHostileDist-nearestDistToHostile);// * instance.pursueMultiplier;
							//action.scoreAlt*=Rand.Range(0.75f, 1.25f);
							
							//Debug.Log(nearestHostileDist+"   "+nearestDistToHostile+"   "+action.scoreAlt+"   "+action.score);
							//Debug.Log(action.scoreAlt+"   "+maxNearestDistToHostile);
							
							if(action.scoreAlt<minNearestDistToHostile) minNearestDistToHostile=action.scoreAlt;
							if(action.scoreAlt>maxNearestDistToHostile) maxNearestDistToHostile=action.scoreAlt;
							//if(action.scoreAlt>maxNearestDistToHostile) maxNearestDistToHostile=nearestDistToHostile;
						}
						
							//~ if(nearest==furthest) action.score+=1;
							//~ else action.score+=Mathf.Abs(nearest-furthest)/(furthest-nearest) * instance.pursueMultiplier;
							
						//float distToNearestHostile=GridManager.GetDistance(walkableList[i], nearestHostileNode);
						//
						//action.score+=Mathf.Max(0, nearestHostileDist-distToNearestHostile)*instance.pursueMultiplier;
					}
				
					actionList.Add(action);
				}

				AddAbilityActions(unit, walkableList[i], coverScore, actionList);
			}
			
			if(IsAggressive(unit)){
				for(int i=0; i<actionList.Count; i++){
					if(!actionList[i].CachedScore()) continue;
					
					float range=maxNearestDistToHostile-minNearestDistToHostile;
					if(range<=0) actionList[i].scoreAlt=1;
					else actionList[i].scoreAlt=1-(actionList[i].scoreAlt-minNearestDistToHostile)/range;
					
					//Debug.Log(i+" alt score - "+actionList[i].scoreAlt+"   "+actionList[i].score);
					//actionList[i].scoreAlt=(maxNearestDistToHostile-actionList[i].scoreAlt);///maxNearestDistToHostile * instance.pursueMultiplier;
					//Debug.Log("             - "+actionList[i].scoreAlt+"   "+maxNearestDistToHostile);
					//~ actionList[i].score+=(100*actionList[i].scoreAlt/maxNearestDistToHostile) * instance.pursueMultiplier;
					
					actionList[i].score+=100 * actionList[i].scoreAlt * instance.pursueMultiplier;
				}
			}
			
			//no target in range but unit has attacked
			//unit has either just destroyed a unit within range or use up or it's attack option
			if(!hasTargetWithinRange && unit.attackThisTurn<=0){	
				if(!unit.CanMove()) return null;
				
				if(IsAggressive(unit) && !GameControl.EnableCoverSystem()){
					//List<Node> path=AStar.SearchWalkableNode(unit.node, nearestHostileNode, unit.canMovePastUnit, unit.canMovePastObs, true);
					List<Node> path=AStar.SearchWalkableNode(unit.node, nearestHostileNode, AStar.BypassUnitCode(unit), unit.canMovePastObs, true);
					if(path.Count>0){
						int idx=Mathf.Clamp(unit.GetMoveRange()-1, 0, path.Count-1);
						AIAction action=new AIAction(path[idx], 0);
						return action;
					}
				}
			}
			
			if(actionList.Count==0) return null;
			
			//sorting the list according to the score
			List<AIAction> newList=new List<AIAction>();
			while(actionList.Count>0){
				int highestIdx=-1;		float highest=-Mathf.Infinity;
				for(int i=0; i<actionList.Count; i++){
					if(actionList[i].score>highest){ highest=actionList[i].score;	highestIdx=i; }
				}
				
				if(highestIdx<0) break;
				
				if(instance.alwaysUseBestOption) return actionList[highestIdx];
				
				newList.Add(actionList[highestIdx]);
				actionList.RemoveAt(highestIdx);
			}
			
			actionList=newList;
			
			if(actionList.Count==0) return null;
			
			if(actionList[0].score>0){
				for(int i=1; i<actionList.Count; i++){
					if(actionList[i].score>0) continue;
					actionList.RemoveAt(i);		i-=1;
				}
			}
			
			if(actionList.Count==0) return null;
			
			int rand=Rand.GetOption(new List<float>{ 1.5f, 0.25f, 0.125f, 0.075f });
			rand=Mathf.Min(rand, actionList.Count-1);
			
			return actionList[rand];
		}
		/*
		public static AIAction __AnalyseAction(Unit unit){
			List<AIAction> actionList=new List<AIAction>();
			
			List<Node> walkableList=GridManager.SetupWalkableList(unit);
			walkableList.Insert(0, unit.node);
			
			for(int i=0; i<walkableList.Count; i++){
				List<Node> attackNodeList=GridManager.GetAttackableList(unit, walkableList[i]);
				//
				
				float coverScore=100 * CheckCover(walkableList[i], unit) * instance.coverMultiplier;
				
				if(attackNodeList.Count>0){
					for(int n=0; n<attackNodeList.Count; n++){
						
						AIAction action=new AIAction(walkableList[i], attackNodeList[n].unit);
						
						Attack attack=new Attack(unit, attackNodeList[n].unit, false, false);
						action.score+=0.5f*(attack.damageHPMin+attack.damageHPMax)*instance.damageMultiplier;
						action.score+=attack.hitChance*100*instance.hitChanceMultiplier;
						action.score+=attack.critChance*100*instance.critChanceMultiplier;
						action.score+=coverScore;
						
						actionList.Add(action);
					}
				}
				else{
					
				}
			}
			
			if(actionList.Count==0){
				if(unit.IsPassive()){
					if(Rand.value()<0.7f) return new AIAction(unit.node);
					else{
						for(int i=1; i<walkableList.Count; i++){
							float coverScore=100 * CheckCover(walkableList[i], unit) * instance.coverMultiplier;
							float dist=GridManager.GetDistance(unit.node, walkableList[i]);
							if(dist==1) actionList.Add(new AIAction(walkableList[i], coverScore));
							else if(dist==2 && Rand.value()<0.3f) actionList.Add(new AIAction(walkableList[i], coverScore));
						}
						
						//if unit is not triggered or cover system is not active,
						//just move randomly, otherwise let the sorting algorithm at the end of the function to choose the node with higeste cover
						if(actionList.Count>0 && (!unit.triggered || !GameControl.EnableCoverSystem())) 
							return actionList[Rand.Range(0, actionList.Count)];
					}
				}
				else if(unit.IsAggressive()){
					float furthestDist=0;	
					List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.facID);
					
					for(int i=0; i<walkableList.Count; i++){
						float nearest=Mathf.Infinity;
						for(int n=0; n<hostileList.Count; n++){
							float dist=GridManager.GetDistance(walkableList[i], hostileList[n].node);
							if(dist>furthestDist) furthestDist=dist;
							if(dist<nearest) nearest=dist;
						}
						
						actionList.Add(new AIAction(walkableList[i], nearest, 100*CheckCover(walkableList[i], unit)*instance.coverMultiplier));
					}
					
					for(int i=0; i<actionList.Count; i++){
						actionList[i].score=instance.pursueMultiplier * (furthestDist-actionList[i].score)/furthestDist;
						actionList[i].score+=actionList[i].scoreAlt;
					}
				}
			}
			
			int highestIdx=-1;		float highest=-1;
			for(int i=0; i<actionList.Count; i++){
				if(actionList[i].score>highest){
					highest=actionList[i].score;	highestIdx=i;
				}
			}
			
			//Debug.Log("AI action: "+actionList.Count+"   "+highestIdx);
			
			if(actionList.Count>0){
				if(highestIdx>=0) return actionList[highestIdx];
				else return actionList[0];
			}
			
			return null;
		}
		*/
		

		private static bool UnitHasUsableAbility(Unit unit){
			if(instance==null || !instance.useAbilities || unit==null || unit.AbilityDisabled()) return false;
			if(unit.abilityList==null) return false;
			for(int i=0; i<unit.abilityList.Count; i++){
				Ability ability=unit.abilityList[i];
				if(ability==null) continue;
				if(ability.IsAvailable()!=Ability._AbilityStatus.Ready) continue;
				if(ability.IsMultipleTargetLock()){
					if(GetMultipleTargetLockTargetsFromNode(unit, unit.node, ability).Count<=0) continue;
				}
				return true;
			}
			return false;
		}

		private static void AddAbilityActions(Unit unit, Node moveNode, float coverScore, List<AIAction> actionList){
			if(instance==null || !instance.useAbilities || unit==null || moveNode==null || actionList==null) return;
			if(unit.AbilityDisabled() || unit.abilityList==null) return;

			for(int i=0; i<unit.abilityList.Count; i++){
				Ability ability=unit.abilityList[i];
				if(ability==null) continue;
				if(ability.IsAvailable()!=Ability._AbilityStatus.Ready) continue;

				if(ability.IsMultipleTargetLock()){
					List<Unit> lockedTargets=GetMultipleTargetLockTargetsFromNode(unit, moveNode, ability);
					if(lockedTargets.Count<=0) continue;
					float score=EvaluateMultipleTargetLockScore(unit, ability, lockedTargets);
					if(score<instance.minimumAbilityScore) continue;
					AIAction action=new AIAction(moveNode, coverScore);
					action.ability=ability;
					action.abilityIdx=i;
					action.abilityTargetNode=lockedTargets[0].node;
					action.abilityLookNode=lockedTargets[0].node;
					action.score+=score*instance.abilityMultiplier;
					actionList.Add(action);
					continue;
				}

				List<Node> targetList=GetPotentialAbilityTargetNodes(unit, moveNode, ability);
				for(int n=0; n<targetList.Count; n++){
					Node tgtNode=targetList[n];
					float score=EvaluateAbilityScore(unit, moveNode, ability, tgtNode);
					if(score<instance.minimumAbilityScore) continue;

					AIAction action=new AIAction(moveNode, coverScore);
					action.ability=ability;
					action.abilityIdx=i;
					action.abilityTargetNode=tgtNode;
					action.abilityLookNode=GetBestAbilityLookNode(unit, moveNode, ability, tgtNode);
					action.score+=score*instance.abilityMultiplier;
					actionList.Add(action);
				}
			}
		}

		private static List<Unit> GetMultipleTargetLockTargetsFromNode(Unit unit, Node sourceNode, Ability ability){
			List<Unit> targetList=new List<Unit>();
			if(unit==null || sourceNode==null || ability==null || !ability.IsMultipleTargetLock()) return targetList;
			
			List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
			if(hostileList==null) return targetList;
			
			int lockRange=ability.multipleTargetLockUseSight ? unit.GetSight() : ability.GetRange();
			for(int i=0; i<hostileList.Count; i++){
				Unit target=hostileList[i];
				if(target==null || target.node==null || target.hp<=0) continue;
				if(target.GetFacID()==unit.GetFacID()) continue;
				
				int dist=GridManager.GetDistance(sourceNode, target.node);
				if(dist<ability.GetRangeMin()) continue;
				if(lockRange>0 && dist>lockRange) continue;
				if(ability.multipleTargetLockRequireLOS && !GridManager.CheckLOS(sourceNode, target.node, unit.GetSight())) continue;
				
				targetList.Add(target);
			}
			
			targetList.Sort((a,b)=>GridManager.GetDistance(sourceNode, a.node).CompareTo(GridManager.GetDistance(sourceNode, b.node)));
			
			int maxTargets=ability.GetMultipleTargetLockMaxTargets();
			if(maxTargets>0 && targetList.Count>maxTargets) targetList.RemoveRange(maxTargets, targetList.Count-maxTargets);
			return targetList;
		}

		private static float EvaluateMultipleTargetLockScore(Unit unit, Ability ability, List<Unit> targetList){
			if(unit==null || ability==null || targetList==null || targetList.Count<=0) return 0;
			float score=0;
			float hpImpact=(ability.GetHPMin()+ability.GetHPMax())*0.5f;
			float apImpact=(ability.GetAPMin()+ability.GetAPMax())*0.25f;
			float hitScore=Mathf.Clamp01(ability.GetHit())*100f*instance.hitChanceMultiplier;
			float critScore=Mathf.Clamp01(ability.GetCritChance())*100f*instance.critChanceMultiplier;
			
			for(int i=0; i<targetList.Count; i++){
				Unit target=targetList[i];
				if(target==null || target.hp<=0) continue;
				if(ability.HasNegativeImpact()){
					score+=(hpImpact+apImpact)*instance.abilityImpactMultiplier;
					score+=hitScore+critScore;
					if(hpImpact>=target.hp) score+=75;
				}
				else if(ability.effectIDList!=null && ability.effectIDList.Count>0){
					score+=ability.effectIDList.Count*25*instance.abilityEffectMultiplier;
				}
				else if(ability.type==Ability._AbilityType.None){
					score+=20*instance.abilityEffectMultiplier;
				}
			}
			
			if(targetList.Count>1) score+=(targetList.Count-1)*40*instance.abilityAOEMultiplier;
			return score;
		}

		private static List<Node> GetPotentialAbilityTargetNodes(Unit unit, Node moveNode, Ability ability){
			List<Node> targetList=new List<Node>();
			if(unit==null || moveNode==null || ability==null) return targetList;

			if(!ability.requireTarget){
				// Non-targeted unit abilities are effectively used on the acting unit/current node.
				// Never let AI use non-targeted offensive abilities, because they can hit the AI's own unit.
				if(IsOffensiveAbility(ability)) return targetList;

				// Allow self-support and special replacement abilities such as ChangeForm/Fusion.
				targetList.Add(moveNode);
				return targetList;
			}

			// Use normal-attack style target gathering for unit-targeted abilities.
			// Offensive abilities start from hostile units only.
			// Friendly/support abilities start from friendly units only.
			// This is safer than scanning every node first because it avoids choosing own-team nodes
			// unless the ability is explicitly a friendly/support ability.
			bool targetEmptyNodeOnly=ability.targetType==Ability._TargetType.EmptyNode;

			if(IsOffensiveAbility(ability) && !targetEmptyNodeOnly){
				List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
				for(int i=0; i<hostileList.Count; i++){
					Unit target=hostileList[i];
					if(target==null || target.node==null) continue;
					TryAddAbilityTargetNode(unit, moveNode, ability, target.node, targetList);
				}
				return targetList;
			}

			if(IsSupportAbility(ability) && !targetEmptyNodeOnly){
				List<Unit> friendlyList=UnitManager.GetAllFriendlyUnits(unit.GetFacID());
				for(int i=0; i<friendlyList.Count; i++){
					Unit target=friendlyList[i];
					if(target==null) continue;

					// If the AI is evaluating a support ability on itself after movement,
					// target the simulated future moveNode, not the unit's old node.
					Node targetNode=(target==unit) ? moveNode : target.node;
					if(targetNode==null) continue;
					TryAddAbilityTargetNode(unit, moveNode, ability, targetNode, targetList);
				}
				return targetList;
			}

			// Empty-node and neutral node abilities still use node-based scanning.
			List<Node> candidates=GridManager.GetNodesWithinDistance(moveNode, ability.GetRange());
			if(candidates==null) candidates=new List<Node>();
			if(!candidates.Contains(moveNode)) candidates.Add(moveNode);

			for(int i=0; i<candidates.Count; i++){
				TryAddAbilityTargetNode(unit, moveNode, ability, candidates[i], targetList);
			}

			return targetList;
		}

		private static void TryAddAbilityTargetNode(Unit unit, Node moveNode, Ability ability, Node node, List<Node> targetList){
			if(unit==null || moveNode==null || ability==null || node==null || targetList==null) return;
			if(targetList.Contains(node)) return;

			int dist=GridManager.GetDistance(moveNode, node);
			if(dist<ability.GetRangeMin() || dist>ability.GetRange()) return;
			if(ability.requireLos && !GridManager.CheckLOS(moveNode, node, unit.GetSight())) return;

			if(IsValidAbilityTarget(unit, moveNode, ability, node)) targetList.Add(node);
		}

		private static bool IsOffensiveAbility(Ability ability){
			if(ability==null) return false;
			return ability.HasNegativeImpact() || ability.switchFaction;
		}

		private static bool IsSupportAbility(Ability ability){
			if(ability==null) return false;
			return ability.HasPositiveImpact() || ability.clearAllEffect;
		}

		private static Unit GetPlannedNodeUnit(Unit actingUnit, Node moveNode, Node checkNode){
			if(actingUnit==null || checkNode==null) return null;

			// During AI evaluation, moveNode is a simulated future position.
			// Grid node.unit is not updated yet, but after movement the acting unit will occupy moveNode.
			// Treat moveNode as occupied by the acting unit so AI will not target its own future node
			// with Hostile/Negative/AllUnit/AllNode abilities.
			if(moveNode!=null && checkNode==moveNode) return actingUnit;

			return checkNode.unit;
		}

		private static bool IsSameFaction(Unit a, Unit b){
			if(a==null || b==null) return false;
			return a.GetFacID()==b.GetFacID();
		}

		private static bool IsUsefulUnitTarget(Unit user, Ability ability, Unit target){
			if(user==null || ability==null || target==null) return false;

			bool sameFac=target.GetFacID()==user.GetFacID();

			// Explicit target type always wins.
			if(ability.targetType==Ability._TargetType.HostileUnit) return !sameFac;
			if(ability.targetType==Ability._TargetType.FriendlyUnit) return sameFac;

			// For AllUnit/AllNode, infer safe target side from the ability impact.
			// This prevents AI from using damage/debuff abilities on its own team.
			if(IsOffensiveAbility(ability)) return !sameFac;
			if(IsSupportAbility(ability)) return sameFac;

			return true;
		}

		private static bool IsValidAbilityTarget(Unit unit, Node moveNode, Ability ability, Node node){
			if(unit==null || moveNode==null || ability==null || node==null) return false;

			if(ability.type==Ability._AbilityType.Line || ability.type==Ability._AbilityType.Cone){
				// Line/Cone target node is mainly used as direction.
				// Only accept the direction if the estimated affected area contains useful targets
				// and does not cause friendly-fire for offensive abilities.
				return DirectionHasUsefulTargets(unit, moveNode, ability, node);
			}

			Unit plannedTargetUnit=GetPlannedNodeUnit(unit, moveNode, node);

			// Important:
			// moveNode is where the AI will stand after moving.
			// Even if node.unit is null now, moveNode will become occupied by the AI unit.
			// Therefore moveNode must NOT be treated as EmptyNode.
			if(ability.targetType==Ability._TargetType.EmptyNode) return plannedTargetUnit==null;

			if(ability.targetType==Ability._TargetType.AllNode){
				if(plannedTargetUnit==null){
					// Empty AllNode target is allowed only if the estimated AOE/direction
					// has useful affected units or this is a real non-damaging node ability.
					if(IsOffensiveAbility(ability) || IsSupportAbility(ability)){
						List<Node> affectedNodes=GetEstimatedAffectedNodes(unit, moveNode, ability, node);
						for(int i=0; i<affectedNodes.Count; i++){
							Unit affectedUnit=GetPlannedNodeUnit(unit, moveNode, affectedNodes[i]);
							if(affectedUnit!=null && IsUsefulUnitTarget(unit, ability, affectedUnit)) return true;
						}
						return false;
					}

					return true;
				}

				return IsUsefulUnitTarget(unit, ability, plannedTargetUnit);
			}

			if(ability.targetType==Ability._TargetType.AllUnit){
				return plannedTargetUnit!=null && IsUsefulUnitTarget(unit, ability, plannedTargetUnit);
			}

			if(ability.targetType==Ability._TargetType.HostileUnit){
				return plannedTargetUnit!=null && !IsSameFaction(plannedTargetUnit, unit);
			}

			if(ability.targetType==Ability._TargetType.FriendlyUnit){
				return plannedTargetUnit!=null && IsSameFaction(plannedTargetUnit, unit);
			}

			return false;
		}

		private static bool DirectionHasUsefulTargets(Unit unit, Node moveNode, Ability ability, Node targetNode){
			List<Node> affectedNodes=GetEstimatedAffectedNodes(unit, moveNode, ability, targetNode);
			if(affectedNodes==null || affectedNodes.Count==0) return false;

			bool hasUseful=false;
			for(int i=0; i<affectedNodes.Count; i++){
				Node node=affectedNodes[i];
				if(node==null) continue;

				Unit affectedUnit=GetPlannedNodeUnit(unit, moveNode, node);
				if(affectedUnit==null) continue;

				bool sameFac=IsSameFaction(affectedUnit, unit);

				// Offensive line/cone should never be selected if it hits own team,
				// including the AI's simulated future node.
				if(IsOffensiveAbility(ability) && sameFac) return false;

				// Support line/cone should never be selected if it benefits enemy.
				if(IsSupportAbility(ability) && !sameFac) return false;

				if(IsUsefulUnitTarget(unit, ability, affectedUnit)) hasUseful=true;
			}

			return hasUseful;
		}

		private static float EvaluateAbilityScore(Unit unit, Node moveNode, Ability ability, Node targetNode){
			if(unit==null || moveNode==null || ability==null) return 0;

			float score=0;

			if(ability.type==Ability._AbilityType.ChangeForm) score+=100*instance.abilityEffectMultiplier;
			if(ability.type==Ability._AbilityType.Fusion) score+=130*instance.abilityEffectMultiplier;
			if(ability.type==Ability._AbilityType.SpawnUnit) score+=70*instance.abilityEffectMultiplier;
			if(ability.type==Ability._AbilityType.DeployBlock) score+=35*instance.abilityEffectMultiplier;
			if(ability.type==Ability._AbilityType.ScanFogOfWar) score+=25*instance.abilityEffectMultiplier;

			List<Node> affectedNodes=GetEstimatedAffectedNodes(unit, moveNode, ability, targetNode);
			if(affectedNodes.Count==0 && targetNode!=null) affectedNodes.Add(targetNode);

			int affectedUsefulCount=0;
			int badFriendlyHitCount=0;
			int badEnemySupportCount=0;

			for(int i=0; i<affectedNodes.Count; i++){
				Node node=affectedNodes[i];
				if(node==null) continue;

				Unit target=GetPlannedNodeUnit(unit, moveNode, node);
				if(target==null) continue;

				bool sameFac=IsSameFaction(target, unit);

				// Hard safety: AI must not choose offensive abilities that hit friendly units.
				// This includes the AI's future moveNode, because after moving it becomes occupied by this unit.
				if(IsOffensiveAbility(ability) && sameFac){
					badFriendlyHitCount+=1;
					continue;
				}

				// Hard safety: AI must not choose support abilities that affect hostile units.
				if(IsSupportAbility(ability) && !sameFac){
					badEnemySupportCount+=1;
					continue;
				}

				if(ability.HasNegativeImpact() && !sameFac){
					float hpImpact=(ability.GetHPMin()+ability.GetHPMax())*0.5f;
					float apImpact=(ability.GetAPMin()+ability.GetAPMax())*0.25f;
					float hitScore=Mathf.Clamp01(ability.GetHit())*100f*instance.hitChanceMultiplier;
					float critScore=Mathf.Clamp01(ability.GetCritChance())*100f*instance.critChanceMultiplier;

					score+=(hpImpact+apImpact)*instance.abilityImpactMultiplier;
					score+=hitScore+critScore;

					if(target.hp>0 && hpImpact>=target.hp) score+=75; // kill bonus
					affectedUsefulCount+=1;
				}
				else if(ability.HasPositiveImpact() && sameFac){
					float missingHP=Mathf.Max(0, target.GetFullHP()-target.hp);
					float missingAP=Mathf.Max(0, target.GetFullAP()-target.ap);
					float hpHeal=Mathf.Min(missingHP, (ability.GetHPMin()+ability.GetHPMax())*0.5f);
					float apHeal=Mathf.Min(missingAP, (ability.GetAPMin()+ability.GetAPMax())*0.5f);

					score+=(hpHeal+apHeal)*instance.abilityImpactMultiplier;
					if(hpHeal>0 || apHeal>0) affectedUsefulCount+=1;
				}
			}

			if(badFriendlyHitCount>0 || badEnemySupportCount>0) return 0;

			if(affectedUsefulCount>1) score+=(affectedUsefulCount-1)*40*instance.abilityAOEMultiplier;

			if(ability.effectIDList!=null && ability.effectIDList.Count>0){
				// Effects follow the selected target side. HostileUnit effects are treated as offensive;
				// FriendlyUnit effects are treated as support. AllUnit/AllNode still need at least one useful target.
				score+=ability.effectIDList.Count*25*instance.abilityEffectMultiplier;
			}
			if(ability.clearAllEffect) score+=30*instance.abilityEffectMultiplier;
			if(ability.switchFaction) score+=90*instance.abilityEffectMultiplier;

			// Penalise abilities that have no useful target, so AI does not waste them or use them on own team.
			if(ability.requireTarget && affectedUsefulCount==0 && (ability.type==Ability._AbilityType.Generic || ability.type==Ability._AbilityType.Line || ability.type==Ability._AbilityType.Cone)) score=0;

			return score;
		}

		private static List<Node> GetEstimatedAffectedNodes(Unit unit, Node moveNode, Ability ability, Node targetNode){
			List<Node> result=new List<Node>();
			if(unit==null || moveNode==null || ability==null || targetNode==null) return result;

			if(ability.type==Ability._AbilityType.Line){
				result=GridManager.GetNodesInALine(moveNode, GridManager.GetAngle(targetNode, moveNode, true), ability.GetRange(), ability.GetRangeMin());
				return result!=null ? result : new List<Node>();
			}

			if(ability.type==Ability._AbilityType.Cone){
				result=GridManager.GetNodesInACone(moveNode, targetNode, ability.GetRange(), ability.GetRangeMin(), ability.fov);
				return result!=null ? result : new List<Node>();
			}

			int aoe=ability.GetAOE();
			if(aoe>0){
				result=GridManager.GetNodesWithinDistance(targetNode, aoe);
				if(result==null) result=new List<Node>();
			}

			if(!result.Contains(targetNode)) result.Add(targetNode);
			return result;
		}


		private static Node GetBestAbilityLookNode(Unit unit, Node moveNode, Ability ability, Node targetNode){
			if(unit==null || moveNode==null || ability==null || targetNode==null) return targetNode;

			// For normal targeted abilities, face the chosen target node.
			if(ability.type!=Ability._AbilityType.Line && ability.type!=Ability._AbilityType.Cone) return targetNode;

			List<Node> affectedNodes=GetEstimatedAffectedNodes(unit, moveNode, ability, targetNode);
			if(affectedNodes==null || affectedNodes.Count==0) return targetNode;

			Node bestNode=null;
			float bestScore=-Mathf.Infinity;

			for(int i=0; i<affectedNodes.Count; i++){
				Node node=affectedNodes[i];
				if(node==null) continue;

				Unit affectedUnit=GetPlannedNodeUnit(unit, moveNode, node);
				if(affectedUnit==null) continue;
				if(!IsUsefulUnitTarget(unit, ability, affectedUnit)) continue;

				float score=0;

				if(IsOffensiveAbility(ability) && !IsSameFaction(affectedUnit, unit)){
					score+=1000;
					score+=Mathf.Max(0, affectedUnit.hp);
				}
				else if(IsSupportAbility(ability) && IsSameFaction(affectedUnit, unit)){
					score+=1000;
					score+=Mathf.Max(0, affectedUnit.GetFullHP()-affectedUnit.hp);
				}
				else{
					continue;
				}

				// Prefer the explicit clicked/selected target if it is useful.
				if(node==targetNode) score+=250;

				// Prefer closer/central targets for cleaner facing.
				score-=GridManager.GetDistance(moveNode, node);

				if(score>bestScore){
					bestScore=score;
					bestNode=node;
				}
			}

			return bestNode!=null ? bestNode : targetNode;
		}

		private static IEnumerator RotateUnitTowardAbilityLookTarget(Unit unit, AIAction action){
			if(unit==null || action==null || action.ability==null) yield break;
			if(!action.ability.IsMultipleTargetLock() && action.ability.type!=Ability._AbilityType.Line && action.ability.type!=Ability._AbilityType.Cone) yield break;

			Node lookNode=action.abilityLookNode!=null ? action.abilityLookNode : action.abilityTargetNode;
			if(lookNode==null) yield break;

			Vector3 lookPos=lookNode.GetPos();
			if(lookNode.unit!=null) lookPos=lookNode.unit.GetTargetPoint();

			float timer=1.25f;
			while(unit!=null && timer>0 && RotateUnitTowards(unit, lookPos)>2f){
				timer-=Time.deltaTime;
				yield return null;
			}
		}

		private static float RotateUnitTowards(Unit unit, Vector3 targetPos){
			if(!Unit.enableRotation || unit==null) return 0;

			Vector3 dir=targetPos-unit.transform.position;
			dir.y=0;
			if(dir.sqrMagnitude<0.001f) return 0;

			Quaternion wantedRot=Quaternion.LookRotation(dir);
			wantedRot=Quaternion.Euler(0, wantedRot.eulerAngles.y, 0);

			float speed=Mathf.Max(1f, unit.moveSpeed)*3f;
			unit.transform.rotation=Quaternion.Slerp(unit.transform.rotation, wantedRot, Time.deltaTime*speed);

			return Quaternion.Angle(unit.transform.rotation, wantedRot);
		}

		public static Vector2 CheckCover(Node node, Unit unit, List<Unit> hostileList){
			if(!GameControl.EnableCoverSystem()) return Vector2.zero;
			if(hostileList==null || hostileList.Count==0) return Vector2.zero;
			
			//consider xcom style side stepping for square grid
			
			int lowestCover=2;	float totalCover=0;
			
			//List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.GetFacID());
			for(int i=0; i<hostileList.Count; i++){
				//~ if(GridManager.GetDistance(hostileList[i].node, node)>hostileList[i].GetAttackRange()) continue;
				//~ //Vector3 dir=hostileList[i].node.GetPos()-node.GetPos();
				//~ //int cover=node.GetCover(Utility.Vector2ToAngle(new Vector2(dir.x, dir.z)));
				//~ int cover=node.GetCover(GridManager.GetAngle(node, hostileList[i].node, false));
				//~ if(cover<lowestCover) lowestCover=cover;
				
				int cover=Attack.GetCover(hostileList[i].node, node);
				if(cover<lowestCover) lowestCover=cover;
				
				totalCover+=cover;
			}
			
			totalCover=totalCover/(float)hostileList.Count * 0.4f;	
			
			//Color color=lowestCover==0 ? Color.red : Color.white ;
			//Debug.DrawLine(node.GetPos(), node.GetPos()+new Vector3(0, 1, 0)*(totalCover), color, 2);
			
			return new Vector2(lowestCover, totalCover);
			
			//~ Vector3 avgPos=Vector3.zero;
			//~ List<Unit> hostileList=UnitManager.GetAllHostileUnits(unit.facID);
			//~ for(int n=0; n<hostileList.Count; n++) avgPos+=hostileList[n].GetPos();
			//~ avgPos/=hostileList.Count;
			
			//~ Vector3 dir=avgPos-node.GetPos();	
			//~ return node.GetCover(Utility.Vector2ToAngle(new Vector2(dir.x, dir.z)));
		}
		
	}
	
	
	
	public class AIAction{
		public Node tgtNode;
		public Unit tgtUnit;
		public float score=0;
		public float scoreAlt=-999;	//used to temporarily cache value

		// Ability action support. If ability is assigned, tgtNode is the move node and abilityTargetNode is the clicked/target node.
		public Ability ability;
		public int abilityIdx=-1;
		public Node abilityTargetNode;
		public Node abilityLookNode;
		
		public AIAction(Node node, Unit tgtU=null){
			tgtNode=node;	tgtUnit=tgtU;
		}
		public AIAction(Node node, float ss, float ssAlt=0){
			tgtNode=node;	score=ss;	scoreAlt=ssAlt;
		}
		
		public bool CachedScore(){ return scoreAlt!=-999; }
		public bool IsAbilityAction(){ return ability!=null && abilityIdx>=0; }
	}
	
}
