
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace TBTK{

	[System.Serializable]
	public class UnitAbilityShootPointSet {
		public List<Transform> shootPointList=new List<Transform>();
		public float shootPointSpacing=-1;	// negative value means use unit.shootPointSpacing
	}

	public class Unit : TBMonoItem {
		
		public static bool enableRotation=true;
		public static bool enableAura=true;
		
		public static bool inspector=true;
		
		[HideInInspector] public bool loadedFromCache=false;
		
		
		public int value=50;

		[Header("Zoids Factory")]
		public UnitRarity rarity=UnitRarity.Common;
		public int factoryCost=100;
		[TextArea] public string unitDescription="";
		public bool isUpgradedUnit=false;

		[Header("Ownership Limit")]
		public bool limitedOwned=false;
		public int ownedLimit=1;
		public bool IsOwnedLimited(){ return limitedOwned; }
		public int GetOwnedLimit(){ return !limitedOwned ? int.MaxValue : Mathf.Max(1, ownedLimit); }
		public string GetOwnedLimitLabel(){ return !limitedOwned ? "Unlimited" : Mathf.Max(1, ownedLimit).ToString(); }
		
		[Space(5)] 
		public int facID;	//in runtime, this also correspond to the faction index in factionList
		public void SetFacID(int id){ facID=id; }
		public int GetBaseFacID(){ return facID ; }
		public int GetFacID(){ return tempFacID>=0 ? tempFacID : facID ; }
		
		public bool playableUnit=false;
		
		public AI._AIBehaviour aiBehaviour=AI._AIBehaviour.aggressive;
		public bool requireTrigger=true;	//when true, unit starts in passive state, then switch to aggressive or evasive (doesnt apply for passive)
		//[HideInInspector] 
		public bool triggered=false;
		
		public bool IsPassive(){ return aiBehaviour==AI._AIBehaviour.passive; }
		public bool IsAggressive(){ return (aiBehaviour==AI._AIBehaviour.aggressive && !requireTrigger) || triggered; }
		
		//public bool IsPassive(){ return aiBehaviour==AI._AIBehaviour.passive || !IsAggressive(); }
		//public bool IsAggressive(){ return aiBehaviour==AI._AIBehaviour.aggressive && (triggered || !requireTrigger); }
		//public bool IsEvasive(){ return aiBehaviour=AI._AIBehaviour.evasive && (triggered || !requireTrigger); }
		
		
		[Space(8)]
		public float hp=10;
		public float ap=2;
		
		//public float GetFullHP(){ return stats.hp; }
		public float GetHPRatio(){ return hp/GetFullHP(); }
		
		//public float GetFullAP(){ return stats.ap; }
		public float GetAPRatio(){ return ap/GetFullAP(); }
		
		public float GetFullHP(){ 			return stats.hp			* GetFullHPMul() 		+ GetFullHPMod(); 									}
		public float GetFullHPMul(){ 	return activeEffectMul.stats.hp 			* PerkManager.GetUnitMulHP(prefabID); 		}
		public float GetFullHPMod(){ 	return activeEffectMod.stats.hp 			+ PerkManager.GetUnitModHP(prefabID); 		}
		
		public float GetFullAP(){ 			return stats.ap			* GetFullAPMul() 		+ GetFullAPMod(); 									}
		public float GetFullAPMul(){ 	return activeEffectMul.stats.ap 			* PerkManager.GetUnitMulAP(prefabID); 		}
		public float GetFullAPMod(){ 	return activeEffectMod.stats.ap 			+ PerkManager.GetUnitModAP(prefabID); 		}
		
		public float GetHPRegen(){ 		return stats.hpRegen			* GetHPRegenMul() 		+ GetHPRegenMod(); 									}
		public float GetHPRegenMul(){ 	return activeEffectMul.stats.hpRegen 	* PerkManager.GetUnitMulHPRegen(prefabID); 		}
		public float GetHPRegenMod(){ return activeEffectMod.stats.hpRegen + PerkManager.GetUnitModHPRegen(prefabID); 		}
		
		public float GetAPRegen(){ 		return stats.apRegen			* GetAPRegenMul() 		+ GetAPRegenMod(); 									}
		public float GetAPRegenMul(){ 	return activeEffectMul.stats.apRegen 	* PerkManager.GetUnitMulAPRegen(prefabID); 		}
		public float GetAPRegenMod(){ return activeEffectMod.stats.apRegen 	+ PerkManager.GetUnitModAPRegen(prefabID); 		}
		
		
		
		public int GetMoveLimit(){ return (int)(stats.moveLimit * GetMoveLimitMul()) + (int)GetMoveLimitMod(); }
		public float GetMoveLimitMul(){ 	return activeEffectMul.stats.moveLimit 	* PerkManager.GetUnitMulMoveLim(prefabID); 		}
		public float GetMoveLimitMod(){ 	return activeEffectMod.stats.moveLimit 	+ PerkManager.GetUnitModMoveLim(prefabID); 		}
		public int GetMoveRemain(){ return GetMoveLimit()-moveThisTurn; }
		public int moveThisTurn=0;
		
		public int GetAttackLimit(){ return (int)(stats.attackLimit * GetAttackLimitMul()) + (int)GetAttackLimitMod(); }
		public float GetAttackLimitMul(){ 		return activeEffectMul.stats.attackLimit 	* PerkManager.GetUnitMulAttackLim(prefabID); 		}
		public float GetAttackLimitMod(){ 	return activeEffectMod.stats.attackLimit 	+ PerkManager.GetUnitModAttackLim(prefabID); 		}
		public int GetAttackRemain(){ return GetAttackLimit()-attackThisTurn; }
		public int attackThisTurn=0;
		
		public int GetCounterLimit(){ return (int)(stats.counterLimit * GetCounterLimitMul()) + (int)GetCounterLimitMod(); }
		public float GetCounterLimitMul(){ 	return activeEffectMul.stats.counterLimit 	* PerkManager.GetUnitMulCounterLim(prefabID); 		}
		public float GetCounterLimitMod(){ 	return activeEffectMod.stats.counterLimit 	+ PerkManager.GetUnitModCounterLim(prefabID); 		}
		public int counterThisTurn=0;
		
		public int GetAbilityLimit(){ return (int)(stats.abilityLimit * GetAbilityLimitMul() + GetAbilityLimitMod()); }
		public float GetAbilityLimitMul(){ 	return activeEffectMul.stats.abilityLimit 	* PerkManager.GetUnitMulAbilityLim(prefabID); 		}
		public float GetAbilityLimitMod(){ 	return activeEffectMod.stats.abilityLimit 	+ PerkManager.GetUnitModAbilityLim(prefabID); 		}	
		public int GetAbilityRemain(){ return GetAbilityLimit()-abilityThisTurn; }
		public int abilityThisTurn=0;
		
		//public const int apPerMove=1;
		//public const int apPerNode=0;
		//public const int apPerAttack=1;
		
		public bool HasTakenAction(){ return (moveThisTurn + attackThisTurn + abilityThisTurn + counterThisTurn) > 0 ; }
		
		[Space(8)]
		public AStar._BypassUnit canMovePastUnit;
		//public bool canMovePastUnit;
		public bool canMovePastObs;
		
		[Space(8)]
		public Transform targetPoint;
		public float radius=0.25f;
		
		public Vector3 GetTargetPoint(){ return targetPoint!=null ? targetPoint.position : GetPos(); }
		public float GetRadius(){ return radius; }
		
		public ShootObject soRange;
		public ShootObject soMelee;
		public float shootPointSpacing=0.1f;
		public List<Transform> shootPointList=new List<Transform>();
		
		public Transform turretPivot;
		public Transform barrelPivot;
		public bool snapAiming;
		public bool aimInXAxis;
		public bool rotateWhileAiming;
		
		private float aimSpeed=7;
		private bool instantRotate=false;
		[Space(5)] public float moveSpeed=5;
		
		
		
		public delegate bool ActionCamCheck (bool actionType);
		public delegate IEnumerator ActionCamStart(Vector3 srcPos, Vector3 tgtPos);
		public delegate IEnumerator ActionCamEnd();
		public delegate IEnumerator ActionCamAbilityTimelineStart(Unit srcUnit, Unit tgtUnit, Ability ability);
		
		public static ActionCamCheck actionCamCheck;
		public static ActionCamStart actionCamStart;
		public static ActionCamEnd actionCamEnd;
		public static ActionCamAbilityTimelineStart actionCamAbilityTimelineStart;
		
		
		void Awake(){
			thisT=transform;
			thisObj=gameObject;
			
			if(shootPointList.Count==0) shootPointList.Add(thisT);
			
			InitAbility();
			InitAura();
			UpdateActiveEffect();
			
			if(turretPivot!=null) defaultTurretRot=turretPivot.localRotation;
			if(barrelPivot!=null) defaultBarrelRot=barrelPivot.localRotation;
			
			triggered=false;
			
			InitAnimation();
		}
		
		void Start(){
			if(GameControl.EnableFogOfWar() && !playableUnit){
				Utility.SetLayerRecursively(thisT, TBTK.GetLayerInvisible());
			}
		}
		
		
		public void NewTurn(bool restoreFullHP=false){	//restoreFullHP is used when the game start
			if(restoreFullHP) hp=GetFullHP();
			
			float oldAP=ap;
			float fullAP=GetFullAP();
			float regenAP=GetAPRegen();
			
			if(GameControl.RestoreAPOnTurn()) ap=fullAP;
			else ap=Mathf.Min(ap+regenAP, fullAP);
			
			hp=Mathf.Min(hp+GetHPRegen(), GetFullHP());
			
			moveThisTurn=0;
			attackThisTurn=0;
			counterThisTurn=0;
			abilityThisTurn=0;
			UpdateStunAnimation();

			Debug.Log("[AP NewTurn] "+name+" AP "+oldAP+" -> "+ap+" / "+fullAP+" regen="+regenAP+" restoreFull="+GameControl.RestoreAPOnTurn()+" turn="+TurnControl.GetTurn()+" round="+TurnControl.GetRound());
		}
		
		
		public bool CanAttack(){
			if(attackThisTurn>=GetAttackLimit()) return false;
			if(GameControl.UseAPToAttack() && GameControl.GetAPPerAttack()>ap) return false;
			return true;
		}
		public bool CanMove(){
			if(moveThisTurn>=GetMoveLimit()) return false;
			if(GameControl.UseAPToMove() && GameControl.GetAPPerMove()>ap) return false;
			//if(GameControl.UseAPToMove() && GameControl.GetAPPerNode()>ap) return false;
			return true;
		}
		public bool CanCounter(Unit tgtUnit){
			if(IsStunned()) return false;
			if(counterThisTurn>=GetCounterLimit()) return false;
			if(GameControl.UseAPToAttack() && GameControl.GetAPPerAttack()>ap) return false;
			
			int targetRange=GridManager.GetDistance(tgtUnit.node, node);
			if(targetRange>GetAttackRange()) return false;
			
			float minAttackRange=GetAttackRangeMin();
			if(minAttackRange>0 && targetRange<minAttackRange) return false;
			
			if(requireLOSToAttack && !GridManager.CheckLOS(node, tgtUnit.node, GetSight())) return false;
			
			return true;
		}
		
		
		
		[Space(8)]
		public bool hasMeleeAttack=true;
		public bool requireLOSToAttack=true;
		
		public int armorType=0;
		
		public int damageType=0;
		public Stats stats;
		
		public int damageTypeMelee=0;
		public Stats statsMelee;
		
		public List<int> auraIDList=new List<int>();
		public List<Effect> auraMulList=new List<Effect>();
		public List<Effect> auraModList=new List<Effect>();
		
		//public List<int> activeAuraIDList=new List<int>();
		//public Effect activeAuraMul;
		//public Effect activeAuraMod;
		
		public Effect GetAura(int idx){
			if(auraIDList==null) return null;
			if(idx<0 || idx>=auraIDList.Count) return null;
			return EffectDB.GetPrefab(auraIDList[idx]);
		}
		
		public bool HasAura(){ return auraIDList.Count>0; }
		
		private bool auraInitiated=false;
		public void InitAura(){
			if(auraInitiated) return;
			auraInitiated=true;
			
			auraMulList.Clear();	auraModList.Clear();
			for(int i=0; i<auraIDList.Count; i++){
				Effect eff=EffectDB.GetPrefab(auraIDList[i]).Clone();
				if(eff.IsMultiplier())	auraMulList.Add(eff);
				else						auraModList.Add(eff);
			}
			
			//activeAuraMod=new Effect();	activeAuraMod.stats.ResetAsModifier();
			//activeAuraMul=new Effect();	activeAuraMul.stats.ResetAsMultiplier();
		}
		
		/*
		private List<Unit> cachedAuraTargetList=new List<Unit>();
		private List<Unit> GetCurrentAuraTargets(){
			int maxRange=0;
			for(int i=0; i<auraIDList.Count; i++){
				Effect eff=EffectDB.GetPrefab(auraIDList[i]);
				if(eff.range>maxRange) maxRange=eff.range;
			}
			
			List<Unit> uList=new List<Unit>( UnitManager.GetAllFriendlyUnits(facID) );
			for(int i=0; i<uList.Count; i++){
				if(uList[i]==this) continue;
				if(GridManager.GetDistance(uList[i].node, node)<=maxRange) continue;
				uList.RemoveAt(i); i-=1;
			}
			
			return uList;
		}
		
		public void UpdateAuraTarget(){		//called when moving into a new node, as well as when the game starts
			if(auraIDList.Count==0) return;
			
			List<Unit> newAuraTargetList=GetCurrentAuraTargets();
			
			for(int i=0; i<newAuraTargetList.Count; i++){
				if(!cachedAuraTargetList.Contains(newAuraTargetList[i])) continue;
				//for(int n=0; n<auraIDList.Count; n++) newAuraTargetList[i].AddAura(auraIDList[n]);
				newAuraTargetList[i].AddAura(auraIDList);
			}
			
			for(int i=0; i<cachedAuraTargetList.Count; i++){
				if(!newAuraTargetList.Contains(newAuraTargetList[i])) continue;
				//for(int n=0; n<auraIDList.Count; n++) cachedAuraTargetList[i].RemoveAura(auraIDList[n]);
				cachedAuraTargetList[i].RemoveAura(auraIDList);
			}
		}
		
		public void AddAura(List<int> aList){
			for(int n=0; n<aList.Count; n++) activeAuraIDList.Add(aList[n]);
			UpdateActiveAura();
		}
		public void RemoveAura(List<int> aList){
			for(int n=0; n<aList.Count; n++) activeAuraIDList.Remove(aList[n]);
			UpdateActiveAura();
		}
		public void UpdateActiveAura(){
			activeAuraMod=new Effect();	activeAuraMod.stats.ResetAsModifier();
			activeAuraMul=new Effect();	activeAuraMul.stats.ResetAsMultiplier();
			
			for(int i=0; i<activeAuraIDList.Count; i++){
				Effect eff=EffectDB.GetPrefab(activeAuraIDList[i]).Clone();
				if(eff.IsMultiplier()) activeAuraMod.stats.ApplyMultiplier(eff.stats);
				else activeAuraMul.stats.ApplyModifier(eff.stats);
			}
		}
		*/
		
		
		
		public bool InDistance(Node tgtNode, int tgtDist){ return GridManager.GetDistance(node, tgtNode)<=tgtDist ; }
		
		public float GetAuraAttackMul(Node tgtNode, float value=1){			for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.attack; } return value;		}
		public float GetAuraDefenseMul(Node tgtNode, float value=1){			for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.defense; } return value;		}
		public float GetAuraHitMul(Node tgtNode, float value=1){					for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.hit; } return value;				}
		public float GetAuraDodgeMul(Node tgtNode, float value=1){			for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.dodge; } return value;		}
		
		public float GetAuraDmgHPMinMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.dmgHPMin; } return value; }
		public float GetAuraDmgHPMaxMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.dmgHPMax; } return value; }
		public float GetAuraDmgAPMinMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.dmgAPMin; } return value; }
		public float GetAuraDmgAPMaxMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.dmgAPMax; } return value; }
		public float GetAuraCritChanceMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.critChance; } return value; }
		public float GetAuraCritReducMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.critReduc; } return value; }
		public float GetAuraCritMul(Node tgtNode, float value=1){				for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.critMultiplier; } return value; }
		
		public float GetAuraCDmgMul(Node tgtNode, float value=1){				for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.cDmgMultip; } return value; }
		public float GetAuraCHitPenaltyMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.cHitPenalty; } return value; }
		public float GetAuraCCritPenaltyMul(Node tgtNode, float value=1){	for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.cCritPenalty; } return value; }
		
		public float GetAuraODmgMul(Node tgtNode, float value=1){				for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.oDmgMultip; } return value; }
		public float GetAuraOHitPenaltyMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.oHitPenalty; } return value; }
		public float GetAuraOCritPenaltyMul(Node tgtNode, float value=1){	for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.oCritPenalty; } return value; }
		
		public float GetAuraAttackRangeMul(Node tgtNode, float value=1){	for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.attackRange; } return value;}
		public float GetAuraAttackRangeMinMul(Node tgtNode, float value=1){for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.attackRangeMin; } return value; }
		public float GetAuraMoveRangeMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.moveRange; } return value; }
		public float GetAuraTurnPriorityMul(Node tgtNode, float value=1){		for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.turnPriority; } return value;}
		public float GetAuraSightMul(Node tgtNode, float value=1){				for(int i=0; i<auraMulList.Count; i++){ if(InDistance(tgtNode, auraMulList[i].range)) value*=auraMulList[i].stats.sight; } return value; }
		
		
		public float GetAuraAttackMod(Node tgtNode, float value=0){			for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.attack; } return value;		}
		public float GetAuraDefenseMod(Node tgtNode, float value=0){			for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.defense; } return value;		}
		public float GetAuraHitMod(Node tgtNode, float value=0){				for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.hit; } return value;			}
		public float GetAuraDodgeMod(Node tgtNode, float value=0){			for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.dodge; } return value;		}
		
		public float GetAuraDmgHPMinMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.dmgHPMin; } return value; }
		public float GetAuraDmgHPMaxMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.dmgHPMax; } return value; }
		public float GetAuraDmgAPMinMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.dmgAPMin; } return value; }
		public float GetAuraDmgAPMaxMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.dmgAPMax; } return value; }
		public float GetAuraCritChanceMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.critChance; } return value; }
		public float GetAuraCritReducMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.critReduc; } return value; }
		public float GetAuraCritMod(Node tgtNode, float value=0){				for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.critMultiplier; } return value; }
		
		public float GetAuraCDmgMod(Node tgtNode, float value=0){			for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.cDmgMultip; } return value; }
		public float GetAuraCHitPenaltyMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.cHitPenalty; } return value; }
		public float GetAuraCCritPenaltyMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.cCritPenalty; } return value; }
		
		public float GetAuraODmgMod(Node tgtNode, float value=0){			for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.oDmgMultip; } return value; }
		public float GetAuraOHitPenaltyMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.oHitPenalty; } return value; }
		public float GetAuraOCritPenaltyMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.oCritPenalty; } return value; }
		
		public float GetAuraAttackRangeMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.attackRange; } return value;}
		public float GetAuraAttackRangeMinMod(Node tgtNode, float value=0){for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.attackRangeMin; } return value; }
		public float GetAuraMoveRangeMod(Node tgtNode, float value=0){		for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.moveRange; } return value; }
		public float GetAuraTurnPriorityMod(Node tgtNode, float value=0){	for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.turnPriority; } return value;}
		public float GetAuraSightMod(Node tgtNode, float value=0){				for(int i=0; i<auraModList.Count; i++){ if(InDistance(tgtNode, auraModList[i].range)) value+=auraModList[i].stats.sight; } return value; }
		
		
		
		
		public List<int> attackEffectIDList=new List<int>();
		
		public List<int> GetRuntimeAttackEffectIDList(){
			return PerkManager.ModifyUnitAttackEffectList(prefabID, attackEffectIDList);
			//return attackEffectIDList;
		}
		
		
		public float GetAttack(){ 			return stats.attack			* GetAttackMul() 		+ GetAttackMod(); 									}
		public float GetAttackMul(){ 		return activeEffectMul.stats.attack 			* PerkManager.GetUnitMulAttack(prefabID)		* UnitManager.GetAuraAttackMul(this, node) ; 		}
		public float GetAttackMod(){ 		return activeEffectMod.stats.attack 			+ PerkManager.GetUnitModAttack(prefabID)		+ UnitManager.GetAuraAttackMod(this, node) ; 		}
		
		public float GetDefense(){ 			return stats.defense		* GetDefenseMul()		+ GetDefenseMod(); 								}
		public float GetDefenseMul(){ 		return activeEffectMul.stats.defense 			* PerkManager.GetUnitMulDefense(prefabID)		* UnitManager.GetAuraDefenseMul(this, node) ; 		}
		public float GetDefenseMod(){ 		return activeEffectMod.stats.defense 		+ PerkManager.GetUnitModDefense(prefabID)	+ UnitManager.GetAuraDefenseMod(this, node) ; 		}
		
		public float GetHit(){ 					return stats.hit				* GetHitMul()				+ GetHitMod(); 										}
		public float GetHitMul(){ 				return activeEffectMul.stats.hit 				* PerkManager.GetUnitMulHit(prefabID)			* UnitManager.GetAuraHitMul(this, node) ; 				}
		public float GetHitMod(){ 			return activeEffectMod.stats.hit 				+ PerkManager.GetUnitModHit(prefabID)			+ UnitManager.GetAuraHitMod(this, node) ; 			}
		
		public float GetDodge(){ 			return stats.dodge			* GetMulDodge()			 + GetModDodge(); 									}
		public float GetMulDodge(){ 		return activeEffectMul.stats.dodge 			* PerkManager.GetUnitMulDodge(prefabID)			* UnitManager.GetAuraDodgeMul(this, node) ; 			}
		public float GetModDodge(){ 		return activeEffectMod.stats.dodge 			+ PerkManager.GetUnitModDodge(prefabID)			+ UnitManager.GetAuraDodgeMod(this, node) ; 		}
		
		public float GetDmgHPMin(){ 		return stats.dmgHPMin		* GetMulDmgHPMin()	 + GetModDmgHPMin(); 							}
		public float GetMulDmgHPMin(){ 	return activeEffectMul.stats.dmgHPMin 		* PerkManager.GetUnitMulDmgHPMin(prefabID)		* UnitManager.GetAuraDmgHPMinMul(this, node) ; 	}
		public float GetModDmgHPMin(){ 	return activeEffectMod.stats.dmgHPMin 		+ PerkManager.GetUnitModDmgHPMin(prefabID)	+ UnitManager.GetAuraDmgHPMinMod(this, node) ; 	}
		
		public float GetDmgHPMax(){ 		return stats.dmgHPMax	* GetMulDmgHPMax()	 + GetModDmgHPMax(); 							}
		public float GetMulDmgHPMax(){ 	return activeEffectMul.stats.dmgHPMax 		* PerkManager.GetUnitMulDmgHPMax(prefabID)		* UnitManager.GetAuraDmgHPMaxMul(this, node) ; 	}
		public float GetModDmgHPMax(){ 	return activeEffectMod.stats.dmgHPMax 		+ PerkManager.GetUnitModDmgHPMax(prefabID)	+ UnitManager.GetAuraDmgHPMaxMod(this, node) ; 	}
		
		public float GetDmgAPMin(){ 		return stats.dmgAPMin		* GetMulDmgAPMin()	 + GetModDmgAPMin(); 							}
		public float GetMulDmgAPMin(){ 	return activeEffectMul.stats.dmgAPMin 		* PerkManager.GetUnitMulDmgAPMin(prefabID)		* UnitManager.GetAuraDmgAPMinMul(this, node) ; 	}
		public float GetModDmgAPMin(){ 	return activeEffectMod.stats.dmgAPMin 		+ PerkManager.GetUnitModDmgAPMin(prefabID)	+ UnitManager.GetAuraDmgAPMinMod(this, node) ; 	}
		
		public float GetDmgAPMax(){ 		return stats.dmgAPMax	* GetMulDmgAPMax()	 + GetModDmgAPMax(); 							}
		public float GetMulDmgAPMax(){ 	return activeEffectMul.stats.dmgAPMax 		* PerkManager.GetUnitMulDmgAPMax(prefabID)		* UnitManager.GetAuraDmgAPMaxMul(this, node) ; 	}
		public float GetModDmgAPMax(){ 	return activeEffectMod.stats.dmgAPMax 		+ PerkManager.GetUnitModDmgAPMax(prefabID)	+ UnitManager.GetAuraDmgAPMaxMod(this, node) ; 	}
		
		public float GetCritChance(){		return stats.critChance	* GetMulCritChance() 	+ GetModCritChance(); 							}
		public float GetMulCritChance(){ 	return activeEffectMul.stats.critChance 		* PerkManager.GetUnitMulCritC(prefabID)				* UnitManager.GetAuraCritChanceMul(this, node) ; 			}
		public float GetModCritChance(){ 	return activeEffectMod.stats.critChance 	+ PerkManager.GetUnitModCritC(prefabID)			+ UnitManager.GetAuraCritChanceMod(this, node) ; 			}
		
		public float GetCritReduc(){			return stats.critReduc		* GetMulCritReduc() 	+ 	GetModCritReduc(); 								}
		public float GetMulCritReduc(){ 	return activeEffectMul.stats.critReduc 		* PerkManager.GetUnitMulCritR(prefabID)				* UnitManager.GetAuraCritReducMul(this, node) ; 			}
		public float GetModCritReduc(){ 	return activeEffectMod.stats.critReduc 		+ PerkManager.GetUnitModCritR(prefabID)			+ UnitManager.GetAuraCritReducMod(this, node) ; 			}
		
		public float GetCritMultiplier(){		return stats.critMultiplier	* GetMulCritMul() 		+	GetModCritMul(); 								}
		public float GetMulCritMul(){ 		return activeEffectMul.stats.critMultiplier 	* PerkManager.GetUnitMulCritM(prefabID)			* UnitManager.GetAuraCritMul(this, node) ; 			}
		public float GetModCritMul(){ 		return activeEffectMod.stats.critMultiplier 	+ PerkManager.GetUnitModCritM(prefabID)			+ UnitManager.GetAuraCritMod(this, node) ; 			}
		
			public float GetCDmgMul(){		return stats.cDmgMultip	* GetMulCDmgMul() 		+ GetModCDmgMul();								}
			public float GetMulCDmgMul(){	return activeEffectMul.stats.cDmgMultip 		* PerkManager.GetUnitMulCDmgMul(prefabID)		* UnitManager.GetAuraCDmgMul(this, node) ; 		}
			public float GetModCDmgMul(){ return activeEffectMod.stats.cDmgMultip 	+ PerkManager.GetUnitModCDmgMul(prefabID)		+ UnitManager.GetAuraCDmgMod(this, node) ; 	}
			
			public float GetCHitPenalty(){	return stats.cHitPenalty	* GetMulCHitPen() 		+ GetModCHitPen();									}
			public float GetMulCHitPen(){ 	return activeEffectMul.stats.cHitPenalty 		* PerkManager.GetUnitMulCHitPen(prefabID)			* UnitManager.GetAuraCHitPenaltyMul(this, node) ; 		}
			public float GetModCHitPen(){ 	return activeEffectMod.stats.cHitPenalty 	+ PerkManager.GetUnitModCHitPen(prefabID)		+ UnitManager.GetAuraCHitPenaltyMod(this, node) ; 		}
			
			public float GetCCritPenalty(){	return stats.cCritPenalty	* GetMulCCritPen() 		+ GetModCCritPen();								}
			public float GetMulCCritPen(){ 	return activeEffectMul.stats.cCritPenalty 	* PerkManager.GetUnitMulCCritPen(prefabID)		* UnitManager.GetAuraCCritPenaltyMul(this, node) ; 		}
			public float GetModCCritPen(){ return activeEffectMod.stats.cCritPenalty 	+ PerkManager.GetUnitModCCritPen(prefabID)		+ UnitManager.GetAuraCCritPenaltyMod(this, node) ; 		}
			
			public float GetODmgMul(){		return stats.oDmgMultip	* GetMulODmgMul() 	+ GetModODmgMul();								}
			public float GetMulODmgMul(){	return activeEffectMul.stats.oDmgMultip 		* PerkManager.GetUnitMulODmgMul(prefabID)		* UnitManager.GetAuraODmgMul(this, node) ; 		}
			public float GetModODmgMul(){ return activeEffectMod.stats.oDmgMultip 	+ PerkManager.GetUnitModODmgMul(prefabID)		+ UnitManager.GetAuraODmgMod(this, node) ; 	}
			
			public float GetOHitPenalty(){	return stats.oHitPenalty	* GetMulOHitPen()		 + GetModOHitPen();								}
			public float GetMulOHitPen(){ 	return activeEffectMul.stats.oHitPenalty 		* PerkManager.GetUnitMulOHitPen(prefabID)			* UnitManager.GetAuraOHitPenaltyMul(this, node) ; 		}
			public float GetModOHitPen(){ 	return activeEffectMod.stats.oHitPenalty 	+ PerkManager.GetUnitModOHitPen(prefabID)		+ UnitManager.GetAuraOHitPenaltyMod(this, node) ; 		}
			
			public float GetOCritPenalty(){	return stats.oCritPenalty	* GetMulOCritPen() 		+GetModOCritPen();									}
			public float GetMulOCritPen(){ 	return activeEffectMul.stats.oCritPenalty 	* PerkManager.GetUnitMulOCritPen(prefabID)		* UnitManager.GetAuraOCritPenaltyMul(this, node) ; 		}
			public float GetModOCritPen(){ return activeEffectMod.stats.oCritPenalty 	+ PerkManager.GetUnitModOCritPen(prefabID)		+ UnitManager.GetAuraOCritPenaltyMod(this, node) ; 	}
		
		public int GetAttackRange(){ 		return (int)Mathf.Round(stats.attackRange	* GetMulARange() 	+ GetModARange()); 						}
		public float GetMulARange(){ 		return activeEffectMul.stats.attackRange 	* PerkManager.GetUnitMulARange(prefabID)			* UnitManager.GetAuraAttackRangeMul(this, node) ; 		}
		public float GetModARange(){ 		return activeEffectMod.stats.attackRange 	+ PerkManager.GetUnitModARange(prefabID)		+ UnitManager.GetAuraAttackRangeMod(this, node) ; 		}
		
		public int GetAttackRangeMin(){ 	return (int)Mathf.Round(stats.attackRangeMin	* GetMulARangeMin() 	+ GetModARangeMin()); 			}
		public float GetMulARangeMin(){ 	return activeEffectMul.stats.attackRangeMin 	* PerkManager.GetUnitMulARangeMin(prefabID)		* UnitManager.GetAuraAttackRangeMinMul(this, node) ; 		}
		public float GetModARangeMin(){ 	return activeEffectMod.stats.attackRangeMin 	+ PerkManager.GetUnitModARangeMin(prefabID)	+ UnitManager.GetAuraAttackRangeMinMod(this, node) ; 		}
		
		public int GetMoveRange(){ 		return (int)Mathf.Round(stats.moveRange	* GetMulMRange() 	+ GetModMRange()); 							}
		public float GetMulMRange(){ 		return activeEffectMul.stats.moveRange 		* PerkManager.GetUnitMulMRange(prefabID)			* UnitManager.GetAuraMoveRangeMul(this, node) ; 		}
		public float GetModMRange(){ 		return activeEffectMod.stats.moveRange 	+ PerkManager.GetUnitModMRange(prefabID)		+ UnitManager.GetAuraMoveRangeMod(this, node) ; 		}
		
		public float GetTurnPriority(){		return stats.turnPriority		* GetMulTPriority()		+ GetModTPriority(); 							}
		public float GetMulTPriority(){ 		return activeEffectMul.stats.turnPriority 		* PerkManager.GetUnitMulTPrioity(prefabID)			* UnitManager.GetAuraTurnPriorityMul(this, node) ; 		}
		public float GetModTPriority(){ 	return activeEffectMod.stats.turnPriority 	+ PerkManager.GetUnitModTPrioity(prefabID)		+ UnitManager.GetAuraTurnPriorityMod(this, node) ; 		}
		
		public int GetSight(){ 					return (int)Mathf.Round(stats.sight				* GetMulSight() 			+ GetModSight()); 						}
		public float GetMulSight(){ 			return activeEffectMul.stats.sight 				* PerkManager.GetUnitMulSight(prefabID)			* UnitManager.GetAuraSightMul(this, node) ; 			}
		public float GetModSight(){ 		return activeEffectMod.stats.sight 			+ PerkManager.GetUnitModSight(prefabID)			+ UnitManager.GetAuraSightMod(this, node) ; 			}
		
		
		
		
		public bool IsStunned(){ return activeEffectMod.stun; }
		public bool AbilityDisabled(){ return activeEffectMod.disableAbility; }
		//public bool HasOverwatch(){ return activeEffectMod.overwatch; }
		
		public float GetAttackM(){ 			return statsMelee.attack			* GetAttackMul() 		+ GetAttackMod(); 			}
		public float GetHitM(){ 				return statsMelee.hit				* GetHitMul()				+ GetHitMod();				}
		public float GetDmgHPMinM(){ 		return statsMelee.dmgHPMin		* GetMulDmgHPMin()	+ GetModDmgHPMin();		}
		public float GetDmgHPMaxM(){		return statsMelee.dmgHPMax		* GetMulDmgHPMax()	+ GetModDmgHPMax(); 	}
		public float GetDmgAPMinM(){ 		return statsMelee.dmgAPMin		* GetMulDmgAPMin()	+ GetModDmgAPMin(); 		}
		public float GetDmgAPMaxM(){		return statsMelee.dmgAPMax		* GetMulDmgAPMax()	+ GetModDmgAPMax(); 	}
		
		public float GetCritChanceM(){		return statsMelee.critChance	* GetMulCritChance() 	+ GetModCritChance(); 	}
		public float GetCritMultiplierM(){	return statsMelee.critMultiplier	* GetMulCritMul() 		+GetModCritMul(); 			}
		
		public int GetAttackRangeMelee(){ return (int)(statsMelee.attackRange); }
		//public int GetAttackRangeMelee(){ return 	(int)(statsMelee.attackRange	*activeEffectMul.stats.attackRange 	+ 	activeEffectMod.stats.attackRange); }
		
		
		
		public bool overwatching;
		public bool HasOverwatch(){ return activeEffectMod.overwatch || overwatching; }
		
		
		[Space(8)]	//faction switch
		public int tempFacID=-1;
		public int tempFacDur=-1;
		public bool tempFacControl=false;
		
		public void SwitchFaction(int newFacID, int dur, bool controllable){
			tempFacID=newFacID;
			tempFacDur=dur;
			tempFacControl=controllable;
			
			UnitManager.AddFacSwitchUnit(this);
		}
		
		
		[Space(8)]
		public List<int> immuneEffectList=new List<int>();
		
		public Effect activeEffectMod;
		public Effect activeEffectMul;
		public List<Effect> effectList=new List<Effect>();
		
		public Effect GetEffect(int idx){ return effectList[idx]; }
		
		public void ApplyEffect(List<int> list){
			if(list.Count==0) return;
			for(int i=0; i<list.Count; i++){
				Effect eff=EffectDB.GetPrefab(list[i]).Clone(true);
				
				List<int> immuneList=PerkManager.ModifyUnitImmuneEffectList(prefabID, immuneEffectList);
				if(immuneList.Contains(list[i])){
					TBTK.TextOverlay("Immuned to "+eff.name, GetPos());
					continue;
				}
				
				eff.hitVisualEffect.Spawn(GetPos(), Quaternion.identity);
				if(eff.activeVisualEffect!=null){
					eff.activeVisualEffect=ObjectPoolManager.Spawn(eff.activeVisualEffect, GetPos(), Quaternion.identity);
					eff.activeVisualEffect.parent=thisT;
				}
				
				eff.durationRemain=eff.duration;
				effectList.Add(eff);
				ApplyEffectImpact(effectList[effectList.Count-1]);
			}
			UpdateActiveEffect();
		}
		
		public void UpdateActiveEffect(){
			activeEffectMod=new Effect();	activeEffectMod.stats.ResetAsModifier();
			activeEffectMul=new Effect();	activeEffectMul.stats.ResetAsMultiplier();
			
			for(int i=0; i<effectList.Count; i++){
				//if(effectList[i]==null){ effectList.RemoveAt(i);	i-=1; continue; }
				
				activeEffectMod.stun|=effectList[i].stun;
				activeEffectMod.disableAbility|=effectList[i].disableAbility;
				activeEffectMod.overwatch|=effectList[i].overwatch;
				
				if(effectList[i].IsMultiplier()){
					activeEffectMul.stats.ApplyMultiplier(effectList[i].stats);
				}
				else{
					activeEffectMod.stats.ApplyModifier(effectList[i].stats);
				}
			}
			
			UpdateStunAnimation();
		}
		
		public void RemoveOverwatch(){
			for(int i=0; i<effectList.Count; i++){
				if(effectList[i].overwatch){ effectList.RemoveAt(i); break; }
			}
			UpdateActiveEffect();
		}
		
		public void IterateCD(){
			bool requireUpdate=false;
			for(int i=0; i<effectList.Count; i++){
				effectList[i].durationRemain-=1;
				if(effectList[i].durationRemain<=0){
					if(effectList[i].activeVisualEffect!=null){
						ObjectPoolManager.Unspawn(effectList[i].activeVisualEffect);
					}
					
					requireUpdate=true;
					effectList.RemoveAt(i);	i-=1;
					continue;
				}
				
				ApplyEffectImpact(effectList[i]);
			}
			if(requireUpdate) UpdateActiveEffect();
			
			if(hp<=0) return;	//UnitManager will call the detroy function
			
			for(int i=0; i<abilityList.Count; i++) abilityList[i].IterateCD();
			
			if(tempFacID>=0){
				tempFacDur-=1;
				if(tempFacDur<=0){
					UnitManager.RemoveFacSwitchUnit(this);
					tempFacID=-1;
				}
			}
		}
		
		public void ApplyEffectImpact(Effect eff){
			if(eff.HasNoImpact()) return;
			
			if(eff.HasPositiveImpact()){
				hp=Mathf.Min(hp+eff.GetRandHPModifier(), GetFullHP());
				ap=Mathf.Min(ap+eff.GetRandAPModifier(), GetFullAP());
			}
			if(eff.HasNegativeImpact()){
				hp-=eff.GetRandHPModifier() * DamageTable.GetMultiplier(eff.damageType, armorType);
				ap-=eff.GetRandAPModifier();
			}
			
			
		}
		
		public void ResetEffect(bool forceUpdate=false){
			if(!forceUpdate && effectList.Count==0) return;
			effectList.Clear();
			UpdateActiveEffect();
		}
		
		
		
		#region ability
		[Space(8)]
		public List<int> abilityIDList=new List<int>();
		public List<Ability> abilityList=new List<Ability>();	//runtime attribute
		public Ability GetAbility(int idx){ return abilityList[idx]; }
		//private int selectedAbIdx=-1;
		
		private bool abilityInitiated=false;
		public void InitAbility(){
			if(abilityInitiated) return;
			abilityInitiated=true;
			
			//abilityIDList=new List<int>{ 0, 1 };
			
			abilityList.Clear();
			
			List<int> extraAbIDList=PerkManager.GetUnitAbilityID(prefabID);
			abilityIDList.AddRange(extraAbIDList);
			
			for(int i=0; i<abilityIDList.Count; i++) AddAbility(abilityIDList[i]);
		}
		public void AddAbility(int abPrefabID){
			abilityList.Add(AbilityUDB.GetPrefab(abPrefabID).Clone());
			abilityList[abilityList.Count-1].Init(this, abilityList.Count-1);
		}
		
		
		public Ability._AbilityStatus SelectAbility(int idx){
			Ability._AbilityStatus abilityStatus=abilityList[idx].IsAvailable();
			if(abilityStatus!=Ability._AbilityStatus.Ready) return abilityStatus;
			
			//~ int usable=abilityList[idx].IsAvailable();
			//~ if(usable!=0) return usable;
			
			if(!abilityList[idx].requireTarget){
				//cast ability on self
				UseAbility(idx, node);
			}
			else{
				//GridManager.SetupAbilityTargetList(this, abilityList[idx]);
				//selectedAbIdx=idx;
				AbilityManager.AbilityTargetModeUnit(this, abilityList[idx]);
			}
			
			return 0;
		}
		
		public void UseAbility(int idx, Node target){ 
			GameControl.UnitUseAbility(this, abilityList[idx], target);
			//StartCoroutine(_UseAbility(abilityList[idx], target)); 
		}

		public List<Node> GetMultipleTargetLockTargetNodes(Ability ability){
			List<Node> targetNodeList=new List<Node>();
			if(ability==null || !ability.IsMultipleTargetLock()) return targetNodeList;
			if(node==null) return targetNodeList;
			
			List<Unit> hostileList=UnitManager.GetAllHostileUnits(GetFacID());
			if(hostileList==null) return targetNodeList;
			
			int lockRange=ability.GetMultipleTargetLockRange();
			for(int i=0; i<hostileList.Count; i++){
				Unit target=hostileList[i];
				if(target==null || target==this || target.node==null || target.hp<=0) continue;
				if(target.GetFacID()==GetFacID()) continue;
				if(target.node==node) continue;
				if(target.node.unit!=target) continue;
				
				int dist=GridManager.GetDistance(node, target.node);
				if(dist<ability.GetRangeMin()) continue;
				if(lockRange>0 && dist>lockRange) continue;
				
				if(ability.multipleTargetLockRequireLOS && !GridManager.CheckLOS(node, target.node, GetSight())) continue;
				
				if(!targetNodeList.Contains(target.node)) targetNodeList.Add(target.node);
			}
			
			targetNodeList.Sort((a,b)=>GridManager.GetDistance(node, a).CompareTo(GridManager.GetDistance(node, b)));
			
			int maxTargets=ability.GetMultipleTargetLockMaxTargets();
			if(maxTargets>0 && targetNodeList.Count>maxTargets){
				targetNodeList.RemoveRange(maxTargets, targetNodeList.Count-maxTargets);
			}
			
			return targetNodeList;
		}

		public List<Unit> GetMultipleTargetLockTargets(Ability ability){
			List<Unit> targetList=new List<Unit>();
			List<Node> nodeList=GetMultipleTargetLockTargetNodes(ability);
			for(int i=0; i<nodeList.Count; i++){
				if(nodeList[i]==null || nodeList[i].unit==null) continue;
				targetList.Add(nodeList[i].unit);
			}
			return targetList;
		}

		private Vector3 GetMultipleTargetLockLookPosition(Node targetNode){
			if(targetNode!=null && targetNode.unit!=null) return targetNode.unit.GetTargetPoint();
			if(targetNode!=null) return targetNode.GetPos();
			return GetTargetPoint();
		}

		private IEnumerator UseMultipleTargetLockAbilityRoutine(Ability ability, bool actionCam, bool startTimelineOnAnimationStart, Unit actionCamTargetUnit){
			List<Node> targetNodeList=GetMultipleTargetLockTargetNodes(ability);
			Debug.Log("[MultipleTargetLock] "+name+" ability="+ability.name+" filtered hostile nodes="+targetNodeList.Count);

			if(actionCam && actionCamStart!=null && targetNodeList.Count>0){
				yield return StartCoroutine(actionCamStart(GetTargetPoint(), GetMultipleTargetLockLookPosition(targetNodeList[0])));
			}

			if(targetNodeList.Count>0){
				Vector3 lookPos=GetMultipleTargetLockLookPosition(targetNodeList[0]);
				while(Rotate(lookPos)>2) yield return null;
			}

			if(startTimelineOnAnimationStart) StartAbilityActionCamTimeline(ability, actionCamTargetUnit);

			if(ability.useAttackSequence){
				float attackDelay=AnimPlayAttack(ability.IsMeleeSkill());
				AudioPlayAttack(ability.IsMeleeSkill());
				if(attackDelay>0) yield return new WaitForSeconds(attackDelay);
			}
			else{
				float animationDelay=AnimPlayAbility(ability.index);
				if(animationDelay>0) yield return new WaitForSeconds(animationDelay);
			}

			bool shouldFireShootObject=ability.useAttackSequence || ability.fireShootObjectWithAbilityAnimation;
			bool useAbilityShootPoint=(ability.skillRangeType==Ability._SkillRangeType.Distance);
			List<Transform> usedShootPointList=useAbilityShootPoint ? GetAbilityShootPointList(ability.index) : shootPointList;

			if(shouldFireShootObject && targetNodeList.Count>0){
				GameObject soObj=ability.shootObject!=null ? ability.shootObject.gameObject : GetShootObject(targetNodeList[0]);
				float launchSpacing=ability.GetMultipleTargetLockShootDelay();
				if(launchSpacing<=0) launchSpacing=GetAbilityShootPointSpacing(ability.index);

				Debug.Log("[MultipleTargetLock] Distributed SO launch from "+name+
				          " shootPoints="+(usedShootPointList!=null ? usedShootPointList.Count : 0)+
				          " targets="+targetNodeList.Count+
				          " sourceFac="+GetFacID());

				yield return StartCoroutine(FireMultipleTargetLockDistributedShootObjects(soObj, targetNodeList, usedShootPointList, launchSpacing));
			}

			for(int i=0; i<targetNodeList.Count; i++){
				Node targetNode=targetNodeList[i];
				if(targetNode==null || targetNode.unit==null || targetNode.unit.hp<=0) continue;
				if(targetNode==node || targetNode.unit==this) continue;
				if(targetNode.unit.GetFacID()==GetFacID()) continue;

				yield return CRoutine.Get().StartCoroutine(ability.HitTarget(targetNode));
			}

			if(actionCam && actionCamEnd!=null) yield return StartCoroutine(actionCamEnd());
		}

		private IEnumerator FireMultipleTargetLockDistributedShootObjects(GameObject soPrefab, List<Node> targetNodeList, List<Transform> customShootPointList=null, float customShootPointSpacing=-1){
			if(soPrefab==null || targetNodeList==null || targetNodeList.Count==0) yield break;

			ShootObject prefabSO=soPrefab.GetComponent<ShootObject>();
			if(prefabSO==null){
				Debug.LogWarning("[MultipleTargetLock] ShootObject prefab missing ShootObject component: "+soPrefab.name);
				yield break;
			}

			List<Unit> targetUnitList=new List<Unit>();
			for(int i=0; i<targetNodeList.Count; i++){
				Node targetNode=targetNodeList[i];
				if(targetNode==null || targetNode.unit==null || targetNode.unit.hp<=0) continue;
				if(targetNode==node || targetNode.unit==this) continue;
				if(targetNode.unit.GetFacID()==GetFacID()) continue;
				if(!targetUnitList.Contains(targetNode.unit)) targetUnitList.Add(targetNode.unit);
			}

			if(targetUnitList.Count==0){
				Debug.LogWarning("[MultipleTargetLock] No valid hostile unit target for distributed shoot object.");
				yield break;
			}

			List<Transform> usedShootPointList=HasValidShootPoint(customShootPointList) ? customShootPointList : shootPointList;
			if(!HasValidShootPoint(usedShootPointList)){
				usedShootPointList=new List<Transform>();
				usedShootPointList.Add(thisT);
			}

			float usedSpacing=customShootPointSpacing>=0 ? customShootPointSpacing : shootPointSpacing;

			waitingForHit=true;

			for(int i=0; i<usedShootPointList.Count; i++){
				Transform shootPoint=usedShootPointList[i]!=null ? usedShootPointList[i] : thisT;
				Unit targetUnit=targetUnitList[i % targetUnitList.Count];

				if(targetUnit==null || targetUnit.node==null || targetUnit.hp<=0){
					continue;
				}

				GameObject sObj=(GameObject)Instantiate(soPrefab, shootPoint.position, shootPoint.rotation);
				ShootObject soInstance=sObj.GetComponent<ShootObject>();

				if(soInstance==null){
					Destroy(sObj);
					continue;
				}

				Debug.Log("[MultipleTargetLock] Missile "+i+
				          " shootPoint="+shootPoint.name+
				          " target="+targetUnit.name+
				          " targetFac="+targetUnit.GetFacID()+
				          " sourceFac="+GetFacID());

				if(i==usedShootPointList.Count-1) soInstance.InitShoot(targetUnit, HitCallback, shootPoint);
				else soInstance.InitShoot(targetUnit, null, shootPoint);

				if(i<usedShootPointList.Count-1 && usedSpacing>0) yield return new WaitForSeconds(usedSpacing);
			}

			while(waitingForHit) yield return null;
		}


		private Unit GetAbilityActionCamTargetUnit(Ability ability, Node tgtNode){
			if(ability==null) return null;

			if(ability.IsMultipleTargetLock()){
				List<Unit> targetList=GetMultipleTargetLockTargets(ability);
				if(targetList!=null && targetList.Count>0) return targetList[0];
				return null;
			}

			Node actionCamNode=tgtNode;
			if(ability.IsLine() && actionCamNode!=null && actionCamNode.abLineParent!=null) actionCamNode=actionCamNode.abLineParent;

			if(actionCamNode!=null && actionCamNode.unit!=null) return actionCamNode.unit;
			return null;
		}

		private bool CanUseAbilityActionCamTimeline(Ability ability){
			return actionCamAbilityTimelineStart!=null && ability!=null && ability.UseActionCamTimeline();
		}

		private bool StartAbilityActionCamTimeline(Ability ability, Unit actionCamTargetUnit){
			if(!CanUseAbilityActionCamTimeline(ability)) return false;

			CRoutine.Get().StartCoroutine(actionCamAbilityTimelineStart(this, actionCamTargetUnit, ability));
			return true;
		}

		public IEnumerator UseAbilityRoutine(Ability ability, Node tgtNode){
			if(ability==null) yield break;

			bool useAbilityActionCamTimeline=CanUseAbilityActionCamTimeline(ability);
			bool startTimelineOnActivation=useAbilityActionCamTimeline && ability.StartActionCamTimelineOnActivation();
			bool startTimelineOnAnimationStart=useAbilityActionCamTimeline && ability.StartActionCamTimelineOnAnimationStart();
			Unit actionCamTargetUnit=GetAbilityActionCamTargetUnit(ability, tgtNode);
			bool actionCam=(!useAbilityActionCamTimeline && actionCamCheck!=null && actionCamStart!=null && actionCamCheck(false));

			if(startTimelineOnActivation){
				StartAbilityActionCamTimeline(ability, actionCamTargetUnit);
			}
			else if(actionCam && tgtNode!=null && !ability.IsMultipleTargetLock()){
				yield return StartCoroutine(actionCamStart(GetTargetPoint(), tgtNode.GetPos()));
			}
			
			ability.Activate();

			bool useAbilityMeleeStep=false;
			Vector3 jrpgAbilityOriginalPos=thisT.position;
			Quaternion jrpgAbilityOriginalRot=thisT.rotation;
			bool waitedForAbilityAnimation=false;
			bool abilityReplacesSourceUnit=(ability.type==Ability._AbilityType.ChangeForm || ability.type==Ability._AbilityType.Fusion);
			
			if(ability.IsMultipleTargetLock()){
				yield return StartCoroutine(UseMultipleTargetLockAbilityRoutine(ability, actionCam, startTimelineOnAnimationStart, actionCamTargetUnit));
				yield break;
			}
			
			if(ability.requireTarget && tgtNode!=null){
				if(ability.IsLine()) tgtNode=tgtNode.abLineParent;

				useAbilityMeleeStep=ability.IsMeleeSkill() && GameControl.JRPGMode();

				if(useAbilityMeleeStep){
					// Ability melee in JRPG mode moves visually only. It does not change node ownership or pathing.
					yield return StartCoroutine(JRPGAbilityMeleeStepToTarget(tgtNode, ability.jrpgMeleeStepDistance));
					AnimPlayMove(false);
					AudioStopMove();
					yield return new WaitForSeconds(0.12f);
				}
				else{
					while(Rotate(tgtNode.GetPos())>2) yield return null;
				}
			
				if(startTimelineOnAnimationStart) StartAbilityActionCamTimeline(ability, actionCamTargetUnit);

				if(ability.useAttackSequence){
					// Distance ability must stay as ranged/default attack even when target is close.
					// Melee ability uses melee attack animation/logic only when Skill Range Type is Melee.
					bool useMelee=ability.IsMeleeSkill();
					bool useAbilityShootPoint=(ability.skillRangeType==Ability._SkillRangeType.Distance);
					float attackDelay=0;

					attackDelay=AnimPlayAttack(useMelee);
					AudioPlayAttack(useMelee);

					if(attackDelay>0) yield return new WaitForSeconds(attackDelay);
	
					GameObject soObj=ability.shootObject!=null ? ability.shootObject.gameObject : GetShootObject(tgtNode);
					List<Transform> usedShootPointList=useAbilityShootPoint ? GetAbilityShootPointList(ability.index) : shootPointList;
					Transform firstShootPoint=GetFirstShootPoint(usedShootPointList);
					Vector3 offset=new Vector3(0, firstShootPoint.position.y-node.GetPos().y, 0);
					yield return StartCoroutine(FireShootObject(soObj, tgtNode, ability.aimAtUnit & ability.type!=Ability._AbilityType.Line, offset, usedShootPointList, GetAbilityShootPointSpacing(ability.index)));
				}
				else{
					float animationDelay=AnimPlayAbility(ability.index);
					if(animationDelay>0) yield return new WaitForSeconds(animationDelay);

					if(ability.fireShootObjectWithAbilityAnimation){
						bool useAbilityShootPoint=(ability.skillRangeType==Ability._SkillRangeType.Distance);
						GameObject soObj=ability.shootObject!=null ? ability.shootObject.gameObject : GetShootObject(tgtNode);
						List<Transform> usedShootPointList=useAbilityShootPoint ? GetAbilityShootPointList(ability.index) : shootPointList;
						Transform firstShootPoint=GetFirstShootPoint(usedShootPointList);
						Vector3 offset=new Vector3(0, firstShootPoint.position.y-node.GetPos().y, 0);
						yield return StartCoroutine(FireShootObject(soObj, tgtNode, ability.aimAtUnit & ability.type!=Ability._AbilityType.Line, offset, usedShootPointList, GetAbilityShootPointSpacing(ability.index)));
					}
				}
			}
			else{
				if(startTimelineOnAnimationStart) StartAbilityActionCamTimeline(ability, actionCamTargetUnit);
				float animationDelay=AnimPlayAbility(ability.index);
				if(animationDelay>0) yield return new WaitForSeconds(animationDelay);
			}
			
			yield return CRoutine.Get().StartCoroutine(ability.HitTarget(tgtNode));
			//AbilityHit(ability, target);

			// ChangeForm/CAS and Fusion destroy this source Unit and spawn a replacement.
			// Do not continue this coroutine on the destroyed Unit, or Unity will throw MissingReferenceException.
			if(abilityReplacesSourceUnit || this==null || thisObj==null){
				if(actionCam && actionCamEnd!=null) yield return CRoutine.Get().StartCoroutine(actionCamEnd());
				yield break;
			}

			// Finish any animation-event controlled attack before returning to the original JRPG position.
			if(waitingForAttackAnimation){
				while(waitingForAttackAnimation) yield return null;
				waitedForAbilityAnimation=true;
			}

			if(useAbilityMeleeStep){
				yield return new WaitForSeconds(0.08f);
				yield return StartCoroutine(JRPGMeleeReturn(jrpgAbilityOriginalPos, jrpgAbilityOriginalRot));
			}
			else if(!waitedForAbilityAnimation && waitingForAttackAnimation){
				while(waitingForAttackAnimation) yield return null;
			}
			
			if(actionCam && actionCamEnd!=null) yield return StartCoroutine(actionCamEnd());
		}

		
		//~ public void AbilityHit(Ability ability, Node target){
			//~ ability.HitTarget(target);
		//~ }
		#endregion
		
		
		
		void CheckMoveSpeed(){
			if(moveSpeed<=0){
				Debug.LogError("Setting Error, unit move speed has been set to 0", gameObject);
				moveSpeed=1;
			}
		}
		
		private float Rotate(Vector3 tgtPos){	//for move
			if(!enableRotation) return 0;
			
			Quaternion wantedRot=Quaternion.LookRotation(tgtPos-thisT.position);
			wantedRot=Quaternion.Euler(0, wantedRot.eulerAngles.y, 0);
			
			if(instantRotate){ thisT.rotation=wantedRot; return 0; }
			
			CheckMoveSpeed();
			
			//turretObject.rotation=Quaternion.Slerp(turretObject.rotation, wantedRot, Time.deltaTime*moveSpeed*2);
			thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRot, Time.deltaTime*moveSpeed*3f);
			
			return Quaternion.Angle(thisT.rotation, wantedRot);
		}
		
		public bool Aiming(Vector3 tgtPoint){
			if(turretPivot==null) return true;
			
			//float elevation=shootObject.GetElevationAngle(shootPoint[0].position, tgtPoint);
			
			if(!aimInXAxis || barrelPivot!=null) tgtPoint.y=turretPivot.position.y;
			Quaternion wantedRot=Quaternion.LookRotation(tgtPoint-turretPivot.position);
			//if(elevation!=0 && aimInXAxis && barrelPivot==null) wantedRot*=Quaternion.Euler(elevation, 0, 0);
			
			if(snapAiming) turretPivot.rotation=wantedRot;
			else turretPivot.rotation=Quaternion.Lerp(turretPivot.rotation, wantedRot, aimSpeed*Time.deltaTime);
			
			if(!aimInXAxis || barrelPivot==null) return Quaternion.Angle(turretPivot.rotation, wantedRot)<2;
			
			Quaternion wantedRotX=Quaternion.LookRotation(tgtPoint-barrelPivot.position);
			//if(elevation!=0) wantedRotX*=Quaternion.Euler(elevation, 0, 0);
			
			if(snapAiming) barrelPivot.rotation=wantedRotX;
			else barrelPivot.rotation=Quaternion.Lerp(barrelPivot.rotation, wantedRotX, aimSpeed*Time.deltaTime*2);
			
			return Quaternion.Angle(turretPivot.rotation, wantedRot)<2 & Quaternion.Angle(barrelPivot.rotation, wantedRotX)<2;
		}
		
		IEnumerator AimRoutine(Node tgtNode, float duration=3){
			while(duration>0){
				if(rotateWhileAiming) Rotate(tgtNode.GetPos());
				if(Aiming(tgtNode.unit!=null ? tgtNode.unit.GetTargetPoint() : tgtNode.GetPos())) break;
				duration-=Time.deltaTime;
				yield return null;
			}
		}
		
		private Quaternion defaultTurretRot;
		private Quaternion defaultBarrelRot;
		IEnumerator ResetAim(){
			while(turretPivot!=null){
				turretPivot.localRotation=Quaternion.Lerp(turretPivot.localRotation, defaultTurretRot, aimSpeed*Time.deltaTime);
				
				bool reset=false;
				
				if(barrelPivot!=null){
					barrelPivot.localRotation=Quaternion.Lerp(barrelPivot.localRotation, defaultBarrelRot, aimSpeed*Time.deltaTime*2);
					reset=(Quaternion.Angle(turretPivot.localRotation, defaultTurretRot)<1 & Quaternion.Angle(barrelPivot.localRotation, defaultBarrelRot)<1);
				}
				else reset=(Quaternion.Angle(turretPivot.localRotation, defaultTurretRot)<1);
				
				if(reset) break;
				
				yield return null;
			}
		}
		

		
		
		
		public IEnumerator MoveRoutine(Node targetNode, float speedMul=1){
			List<Node> path=AStar.SearchWalkableNode(node, targetNode, AStar.BypassUnitCode(this), canMovePastObs, true);
			//List<Node> path=AStar.SearchWalkableNode(node, targetNode, canMovePastUnit, canMovePastObs, true);
			//while(path.Count>GetMoveRange()) path.RemoveAt(path.Count-1);
			
			CheckMoveSpeed();
			
			moveThisTurn+=1;

			// Only spend AP for movement when the GameControl setting allows it.
			// Previously AP was reduced even when Use AP To Move was disabled.
			if(GameControl.UseAPToMove()){
				ap-=GameControl.GetAPPerMove();//+path.Count*GameControl.GetAPPerNode();
				for(int i=0; i<path.Count; i++) ap-=path[i].cost;
			}
			
			waitingForMoveRoutine=true;
			
			//Debug.Log("MoveRoutine  "+path.Count+"   "+node.GetPos()+"   "+targetNode.GetPos());
			
			Unit cachedUnit=null;	//for when unit can goes pass an occupied node
			
			//if(auraIDList.Count>0) cachedAuraTargetList=GetCurrentAuraTargets();
			
			AnimPlayMove(true);	AudioPlayMove();
			
			while(path.Count>0){
				if(!IsVisible() && !path[0].IsVisible()){
					thisT.position=path[0].GetPos();
				}
				else{
					while(Rotate(path[0].GetPos())>5) yield return null;
					
					while(true){
						if(enableRotation){
							Quaternion wantedRot=Quaternion.LookRotation(path[0].GetPos()-thisT.position);
							wantedRot=Quaternion.Euler(0, wantedRot.eulerAngles.y, 0);
							thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRot, Time.deltaTime*moveSpeed*speedMul);
						}
						
						float dist=Vector3.Distance(thisT.position, path[0].GetPos());
						
						float currentSpeed=moveSpeed*Time.deltaTime*speedMul;
						Vector3 dir=(path[0].GetPos()-thisT.position).normalized;
						thisT.Translate(dir*currentSpeed, Space.World);
						
						if(dist<currentSpeed*2f) break;
						
						
						//old code, might canuse jerky movement
						//if(dist<0.01f) break;
						
						//Vector3 dir=(path[0].GetPos()-thisT.position).normalized;
						//thisT.Translate(dir*Mathf.Min(moveSpeed*Time.deltaTime*speedMul, dist), Space.World);
						
						yield return null;
					}
				}
				
				
				node.unit=cachedUnit;	cachedUnit=null;		
				
				node=path[0];	
				
				if(path[0].unit!=null) cachedUnit=path[0].unit;
				path[0].unit=this;
				
				GridManager.SetupFogOfWar();
				UnitManager.CheckAITrigger(this);
				
				yield return StartCoroutine(CheckOverwatch());
				if(hp<=0) break;	//if destroyed by overwatch
				
				path.RemoveAt(0);
			}
			
			AnimPlayMove(false);	AudioStopMove();
			
			//if(auraIDList.Count>0) UpdateAuraTarget();
			
			if(node.collectible!=null) yield return StartCoroutine(node.collectible.Trigger(this));
			
			//ApplyAttack(50, 0);
			
			//UnitManager.SelectUnit(this);
			
			if(IsVisible()) yield return new WaitForSeconds(0.2f);
			
			waitingForMoveRoutine=false;
		}
		
		public IEnumerator CheckOverwatch(){
			List<Unit> hostileList=UnitManager.GetAllHostileUnits(facID);
			for(int i=0; i<hostileList.Count; i++){
				if(!hostileList[i].HasOverwatch()) continue;
				
				if(GameControl.UseAPToAttack() && GameControl.GetAPPerAttack()>hostileList[i].ap) continue;
			
				int targetRange=GridManager.GetDistance(node, hostileList[i].node);
				if(targetRange>hostileList[i].GetAttackRange()) continue;
				
				float minAttackRange=hostileList[i].GetAttackRangeMin();
				if(minAttackRange>0 && targetRange<minAttackRange) continue;
				
				if(requireLOSToAttack && !GridManager.CheckLOS(node, hostileList[i].node, GetSight())) continue;
				
				yield return StartCoroutine(hostileList[i].Overwatch(this));
				if(hp<=0) yield break;
			}
		}
		public IEnumerator Overwatch(Unit unit){
			yield return StartCoroutine(AttackRoutine(unit.node, false, true));
		}
		
		
		public bool CheckUseMeleeAttack(Node tgtNode){
			if(!hasMeleeAttack) return false;
			return GridManager.GetDistance(node, tgtNode)<=GetAttackRangeMelee();
		}
		public GameObject GetShootObject(Node tgtNode){
			bool useMelee=CheckUseMeleeAttack(tgtNode);
			if(useMelee && soMelee!=null) return soMelee.gameObject;
			if(!useMelee && soRange!=null) return soRange.gameObject;
			
			//Debug.LogWarning("No ShootObject - you need to assign shoot-object to unit prefab for attack to work");
			
			return GetDummySO();
			//return CheckUseMeleeAttack(tgtNode) ? soMelee.gameObject : soRange.gameObject ;
		}
		
		public IEnumerator AttackRoutine(Node targetNode, bool isCounter=false, bool isOverwatch=false){
			if(!isOverwatch){
				if(!isCounter){
					attackThisTurn+=1;

					// Only spend AP for attack when the GameControl setting allows it.
					// Previously AP was reduced even when Use AP To Attack was disabled.
					if(GameControl.UseAPToAttack()){
						ap-=GameControl.GetAPPerAttack();
					}
				}
				else{
					counterThisTurn+=1;
				}
			}
			else{
				RemoveOverwatch();
			}

			if(!isCounter && !isOverwatch && GameControl.EndMoveAfterAttack()) EndAllAction();

			bool actionCam=(actionCamCheck!=null && actionCamStart!=null && actionCamCheck(true));
			if(actionCam) yield return StartCoroutine(actionCamStart(GetTargetPoint(), targetNode.GetPos()));

			bool useMelee=CheckUseMeleeAttack(targetNode);
			bool useJRPGMeleeStep=useMelee && GameControl.JRPGMode() && !isOverwatch;
			Vector3 jrpgOriginalPos=thisT.position;
			Quaternion jrpgOriginalRot=thisT.rotation;
			bool waitedForAttackAnimation=false;
			bool targetDestroyed=false;

			if(useJRPGMeleeStep){
				// Move first, then stop completely before attack animation starts
				yield return StartCoroutine(JRPGMeleeStepToColumn2(targetNode));
				AnimPlayMove(false);
				AudioStopMove();
				yield return new WaitForSeconds(0.12f);
			}

			yield return StartCoroutine(AimRoutine(targetNode));

			float attackDelay=AnimPlayAttack(useMelee);
			AudioPlayAttack(useMelee);

			if(attackDelay>0) yield return new WaitForSeconds(attackDelay);

			yield return StartCoroutine(FireShootObject(GetShootObject(targetNode), targetNode, true));

			if(targetNode.unit!=null){
				Attack attack=new Attack(this, targetNode.unit, null, isCounter, isOverwatch);	

				if(attack.hit){
					if(!useMelee) effectAttackHit.Spawn(targetNode.unit.GetTargetPoint(), Quaternion.identity);
					else effectAttackHitMelee.Spawn(targetNode.unit.GetTargetPoint(), Quaternion.identity);
				}

				targetDestroyed=targetNode.unit.ApplyAttack(attack);
			}

			// In JRPG melee mode, finish the attack animation before moving back.
			// This prevents movement animation from overlapping attack animation.
			if(waitingForAttackAnimation){
				while(waitingForAttackAnimation) yield return null;
				waitedForAttackAnimation=true;
			}

			if(useJRPGMeleeStep){
				yield return new WaitForSeconds(0.08f);
				yield return StartCoroutine(JRPGMeleeReturn(jrpgOriginalPos, jrpgOriginalRot));
			}

			if(targetNode.unit!=null){
				if(GameControl.EnableCounterAttack() && !targetDestroyed && !isCounter && !isOverwatch && targetNode.unit.CanCounter(this)){
					waitingForCounter=true;
					yield return StartCoroutine(targetNode.unit.AttackRoutine(node, true, false));
				}
			}

			if(actionCam && actionCamEnd!=null) yield return StartCoroutine(actionCamEnd());

			if(!waitedForAttackAnimation){
				if(waitingForAttackAnimation){ while(waitingForAttackAnimation) yield return null; }
				else yield return new WaitForSeconds(0.2f);
			}

			if(turretPivot!=thisT) StartCoroutine(ResetAim());

			waitingForCounter=false;
		}

		private IEnumerator JRPGMeleeStepToColumn2(Node targetNode){
			if(targetNode==null) yield break;

			Vector3 originalPos=thisT.position;
			Vector3 attackPos=GetJRPGColumn2AttackPosition(targetNode);
			attackPos.y=originalPos.y;

			while(Rotate(attackPos)>5) yield return null;
			yield return StartCoroutine(JRPGMoveVisualTo(attackPos, 1.5f));

			// Make sure the unit faces the target before attacking
			if(targetNode.unit!=null){
				Vector3 lookDir=targetNode.unit.GetTargetPoint()-thisT.position;
				lookDir.y=0;
				if(lookDir.sqrMagnitude>0.01f){
					thisT.rotation=Quaternion.LookRotation(lookDir);
				}
			}
		}

		private IEnumerator JRPGAbilityMeleeStepToTarget(Node targetNode, float distanceFromTargetInNodes=2f){
			if(targetNode==null) yield break;

			Vector3 attackPos=GetJRPGAbilityMeleeAttackPosition(targetNode, distanceFromTargetInNodes);

			while(Rotate(attackPos)>5) yield return null;
			yield return StartCoroutine(JRPGMoveVisualTo(attackPos, 1.5f));

			Vector3 lookTarget=targetNode.GetPos();
			if(targetNode.unit!=null) lookTarget=targetNode.unit.GetTargetPoint();

			Vector3 lookDir=lookTarget-thisT.position;
			lookDir.y=0;
			if(lookDir.sqrMagnitude>0.01f){
				thisT.rotation=Quaternion.LookRotation(lookDir);
			}
		}

		private Vector3 GetJRPGAbilityMeleeAttackPosition(Node targetNode, float distanceFromTargetInNodes=2f){
			Vector3 startPos=thisT.position;
			Vector3 targetPos=targetNode!=null ? targetNode.GetPos() : startPos;
			if(targetNode!=null && targetNode.unit!=null) targetPos=targetNode.unit.GetTargetPoint();

			Vector3 dir=startPos-targetPos;
			dir.y=0;

			if(dir.sqrMagnitude<0.01f){
				dir=-thisT.forward;
				dir.y=0;
			}

			dir.Normalize();
			float nodeSize=GridManager.GetNodeSize();
			float distance=Mathf.Max(0.05f, distanceFromTargetInNodes)*nodeSize;
			Vector3 attackPos=targetPos + dir*distance;
			attackPos.y=startPos.y;

			return attackPos;
		}

		private Vector3 GetJRPGColumn2AttackPosition(Node targetNode){
			// In a 3-column JRPG layout, column 2 is the middle column.
			// TBTK uses zero-based idxX, so column 2 = idxX 1.
			int middleColumn=GridManager.DimensionX()/2;
			int targetRow=targetNode!=null ? targetNode.idxZ : node.idxZ;
			Node column2Node=GridManager.GetNode(middleColumn, targetRow);

			if(column2Node!=null){
				return column2Node.GetPos();
			}

			// Fallback: move to the midpoint between attacker and target.
			Vector3 startPos=thisT.position;
			Vector3 targetPos=targetNode!=null ? targetNode.GetPos() : startPos;
			return Vector3.Lerp(startPos, targetPos, 0.5f);
		}

		private IEnumerator JRPGMeleeReturn(Vector3 originalPos, Quaternion originalRot){
			yield return StartCoroutine(JRPGMoveVisualTo(originalPos, 1.5f));
			thisT.rotation=originalRot;
		}

		private IEnumerator JRPGMoveVisualTo(Vector3 targetPos, float speedMul=1.5f){
			CheckMoveSpeed();
			AnimPlayMove(true);
			AudioPlayMove();

			while(Vector3.Distance(thisT.position, targetPos)>0.04f){
				if(enableRotation){
					Vector3 lookDir=targetPos-thisT.position;
					lookDir.y=0;
					if(lookDir.sqrMagnitude>0.01f){
						Quaternion wantedRot=Quaternion.LookRotation(lookDir);
						wantedRot=Quaternion.Euler(0, wantedRot.eulerAngles.y, 0);
						thisT.rotation=Quaternion.Slerp(thisT.rotation, wantedRot, Time.deltaTime*moveSpeed*speedMul);
					}
				}

				float currentSpeed=moveSpeed*Time.deltaTime*speedMul;
				thisT.position=Vector3.MoveTowards(thisT.position, targetPos, currentSpeed);
				yield return null;
			}

			thisT.position=targetPos;
			AnimPlayMove(false);
			AudioStopMove();
		}
		public IEnumerator FireShootObject(GameObject soPrefab, Node tgtNode, bool aimAtUnit, Vector3 offset=default(Vector3), List<Transform> customShootPointList=null, float customShootPointSpacing=-1){
			waitingForHit=true;
			
			List<Transform> usedShootPointList=HasValidShootPoint(customShootPointList) ? customShootPointList : shootPointList;
			if(!HasValidShootPoint(usedShootPointList)){
				usedShootPointList=new List<Transform>();
				usedShootPointList.Add(thisT);
			}
			
			float usedSpacing=customShootPointSpacing>=0 ? customShootPointSpacing : shootPointSpacing;
			
			for(int i=0; i<usedShootPointList.Count; i++){
				Transform shootPoint=usedShootPointList[i]!=null ? usedShootPointList[i] : thisT;
				GameObject sObj=(GameObject)Instantiate(soPrefab, shootPoint.position, shootPoint.rotation);
				ShootObject soInstance=sObj.GetComponent<ShootObject>();
				
				if(aimAtUnit && tgtNode.unit!=null){
					if(i==usedShootPointList.Count-1) soInstance.InitShoot(tgtNode.unit, HitCallback, shootPoint);
					else soInstance.InitShoot(tgtNode.unit, null, shootPoint);
				}
				else{
					if(i==usedShootPointList.Count-1) soInstance.InitShoot(tgtNode, HitCallback, shootPoint, offset);
					else soInstance.InitShoot(tgtNode, null, shootPoint, offset);
				}
				
				if(i<usedShootPointList.Count-1) yield return new WaitForSeconds(usedSpacing);
			}
			
			while(waitingForHit) yield return null;
		}

		public IEnumerator FireShootObject(GameObject soPrefab, Unit tgtUnit, List<Transform> customShootPointList=null, float customShootPointSpacing=-1){
			if(soPrefab==null || tgtUnit==null || tgtUnit==this || tgtUnit.node==null || tgtUnit.hp<=0){
				yield break;
			}

			waitingForHit=true;

			List<Transform> usedShootPointList=HasValidShootPoint(customShootPointList) ? customShootPointList : shootPointList;
			if(!HasValidShootPoint(usedShootPointList)){
				usedShootPointList=new List<Transform>();
				usedShootPointList.Add(thisT);
			}

			float usedSpacing=customShootPointSpacing>=0 ? customShootPointSpacing : shootPointSpacing;

			for(int i=0; i<usedShootPointList.Count; i++){
				Transform shootPoint=usedShootPointList[i]!=null ? usedShootPointList[i] : thisT;
				GameObject sObj=(GameObject)Instantiate(soPrefab, shootPoint.position, shootPoint.rotation);
				ShootObject soInstance=sObj.GetComponent<ShootObject>();

				if(soInstance==null){
					Destroy(sObj);
					continue;
				}

				if(i==usedShootPointList.Count-1) soInstance.InitShoot(tgtUnit, HitCallback, shootPoint);
				else soInstance.InitShoot(tgtUnit, null, shootPoint);

				if(i<usedShootPointList.Count-1) yield return new WaitForSeconds(usedSpacing);
			}

			while(waitingForHit) yield return null;
		}
		
		private bool waitingForMoveRoutine=false;
		private bool waitingForCounter=false;
		private bool waitingForHit=false;
		public void HitCallback(){ waitingForHit=false; }
		
		
		//public bool ApplyAttack(Unit srcUnit, bool isCounter, bool isOverwatch){
		public bool ApplyAttack(Attack attack){
			if(!playableUnit && !triggered) triggered=true;
			
			//Attack attack=new Attack(srcUnit, this, null, isCounter, isOverwatch);	
			Overlay(attack);
			bool destroyed=_ApplyAttack(attack.damageHP, attack.damageAP);
			if(!destroyed) ApplyEffect(attack.srcUnit.GetRuntimeAttackEffectIDList());
			return destroyed;
		}
		public bool ApplyAttack(Ability srcAbility){
			bool destroyed=false;
			bool applyEffect=Rand.value()<srcAbility.GetEffHitChance();
			
			if(srcAbility.clearAllEffect) ResetEffect();
			
			if(srcAbility.HasNegativeImpact()){
				if(srcAbility.factorInTargetStats){
					Attack attack=new Attack(srcAbility, this);	Overlay(attack);
					destroyed=_ApplyAttack(attack.damageHP, attack.damageAP);
				}
				else{
					float dmg=srcAbility.GetRandHPModifier() * DamageTable.GetMultiplier(srcAbility.damageType, armorType);
					TBTK.TextOverlay(Mathf.Round(dmg).ToString("f0"), GetPos());
					_ApplyAttack(dmg);
					ap-=srcAbility.GetRandAPModifier();
				}
			}
			else if(srcAbility.HasPositiveImpact()){
				//TBTK.TextOverlay(Mathf.Round(srcAbility.GetRandHPModifier()).ToString("f0"), GetPos());	//show heal value
				hp=Mathf.Min(hp+srcAbility.GetRandHPModifier(), GetFullHP());
				ap=Mathf.Min(ap+srcAbility.GetRandAPModifier(), GetFullAP());
			}
			
			if(!destroyed && applyEffect){
				ApplyEffect(srcAbility.GetRuntimeEffectIDList());
				
				if(srcAbility.switchFaction && srcAbility.facID!=facID){
					SwitchFaction(srcAbility.facID, srcAbility.GetDuration(), srcAbility.switchFacControllable);
				}
			}
			
			return destroyed;
		}
		public bool _ApplyAttack(float damageHP, float damageAP=0){
			if(damageAP>0) ap=Mathf.Max(0, ap-damageAP);
			
			if(damageHP<=0) return false;
			
			AnimPlayHit();		AudioPlayHit();
			
			hp-=damageHP;
			
			if(hp<=0){
				hp=0;
				StartCoroutine(DestroyRoutine());
			}
			
			return hp<=0;
		}
		
		
		public void Overlay(Attack attack){ StartCoroutine(_Overlay(attack)); }
		public IEnumerator _Overlay(Attack attack){
			if(attack.hit){
				if(attack.damageAP>0){
					if(attack.crit) TBTK.TextOverlay("<i>Critical -"+Mathf.Round(attack.damageAP).ToString("f0")+"AP</i>", GetPos()+new Vector3(0, .5f, 0));//, new Color(0, .6f, .6f, 1));
					else TBTK.TextOverlay("<i>-"+Mathf.Round(attack.damageAP).ToString("f0")+"AP</i>", GetPos()+new Vector3(0, .5f, 0));//, new Color(.2f, 1f, 1f, 1));
					
					yield return new WaitForSeconds(0.15f);
				}
				
				if(attack.damageHP>0){
					if(attack.crit) TBTK.TextOverlay("Critical -"+Mathf.Round(attack.damageHP).ToString("f0")+"HP", GetPos());//, new Color(1f, .6f, .3f, 1));
					else TBTK.TextOverlay("-"+Mathf.Round(attack.damageHP).ToString("f0")+"HP", GetPos());//, new Color(1f, .5f, .5f, 1));
				}
			}
			else TBTK.TextOverlay("Missed", GetPos());
			
			yield return null;
		}
		
		
		[Space(8)] public bool dead=false;
		public IEnumerator DestroyRoutine(){
			node.unit=null;
			UnitManager.UnitDestroyed(this);
			
			//yield return null;	//wait a frame for all on-going coroutine to end
			while(waitingForCounter) yield return null;	
			while(waitingForMoveRoutine) yield return null;	
			
			effectOnDestroyed.Spawn(GetTargetPoint());
			
			float delay=Mathf.Max(AudioPlayDestroy(), AnimPlayDestroyed());
			if(delay>0) yield return new WaitForSeconds(delay);
			
			yield return null;
			
			Destroy(thisObj);
		}
		
		
		public bool IsAllActionCompleted(){
			if(hp<=0) return true;
			
			if(CanMove()) return false; 
			if(CanAttack()) return false; 
			
			for(int i=0; i<abilityList.Count; i++){
				if(abilityList[i].IsAvailable()==0) return false; 
			}
			
			return true;
		}
		
		public void EndAllAction(){
			// End remaining actions without forcing AP to 0 when AP cost systems are disabled.
			// This fixes AP suddenly becoming 0 after one attack when End Move After Attack is enabled.
			/*if(GameControl.UseAPToMove() || GameControl.UseAPToAttack()){
				ap=0;
			}*/

			moveThisTurn=(int)stats.moveLimit;		
			//attackThisTurn=(int)stats.attackLimit;			//dont end attack incase the unit can do two attack in a single turn
			abilityThisTurn=(int)stats.abilityLimit;
		}
		
		
		public bool IsVisible(){ return thisObj.layer!=TBTK.GetLayerInvisible(); }
		
		
		private static GameObject dummySO;
		public static GameObject GetDummySO(){
			if(dummySO==null){
				dummySO=new GameObject("Dummy ShootObject");
				ShootObject so=dummySO.AddComponent<ShootObject>();
				so.type=ShootObject._Type.Effect;
				so.effectDuration=0.25f;
			}
			
			return dummySO;
		}
		
		
		
		
		[Header("Visual Effects")]
		public VisualObject effectAttackHit=new VisualObject();
		public VisualObject effectAttackHitMelee=new VisualObject();
		
		public VisualObject effectOnDestroyed=new VisualObject();
		
		
		#region animation
		[Header("Animation")]
		public Transform animatorT;
		protected Animator animator;
		
		[Space(5)]
		public AnimationClip clipIdle;
		public AnimationClip clipMove;
		public AnimationClip clipHit;
		public AnimationClip clipDestroyed;
		
		[Space(5)]
		[Header("Status Animation")]
		public bool useStunAnimation=true;
		public string stunAnimatorBool="Stunned";
		public AnimationClip clipStunned;
		
		public AnimationClip clipAttackRange;
		public AnimationClip clipAttackMelee;
		public float animAttackDelayRange=0;
		public float animAttackDelayMelee=0;
		
		public List<AnimationClip> clipAbilityList=new List<AnimationClip>();
		public List<float> animAbilityDelayList=new List<float>();
		public List<UnitAbilityShootPointSet> abilityShootPointSetList=new List<UnitAbilityShootPointSet>();
		
		private void EnsureAbilityShootPointSetList(){
			int count=Mathf.Max(clipAbilityList.Count, 6);
			while(abilityShootPointSetList.Count<count) abilityShootPointSetList.Add(new UnitAbilityShootPointSet());
			for(int i=0; i<abilityShootPointSetList.Count; i++){
				if(abilityShootPointSetList[i]==null) abilityShootPointSetList[i]=new UnitAbilityShootPointSet();
			}
		}

		private bool HasAbilityAnimation(int idx){
			return idx>=0 && clipAbilityList.Count>idx && clipAbilityList[idx]!=null;
		}

		private bool HasValidShootPoint(List<Transform> list){
			if(list==null || list.Count==0) return false;
			for(int i=0; i<list.Count; i++){
				if(list[i]!=null) return true;
			}
			return false;
		}

		private List<Transform> GetAbilityShootPointList(int abilityIdx){
			EnsureAbilityShootPointSetList();
			if(abilityIdx>=0 && abilityShootPointSetList.Count>abilityIdx && abilityShootPointSetList[abilityIdx]!=null && HasValidShootPoint(abilityShootPointSetList[abilityIdx].shootPointList)) return abilityShootPointSetList[abilityIdx].shootPointList;
			return shootPointList;
		}

		private float GetAbilityShootPointSpacing(int abilityIdx){
			EnsureAbilityShootPointSetList();
			if(abilityIdx>=0 && abilityShootPointSetList.Count>abilityIdx && abilityShootPointSetList[abilityIdx]!=null) return abilityShootPointSetList[abilityIdx].shootPointSpacing;
			return -1;
		}

		private Transform GetFirstShootPoint(List<Transform> list){
			if(list!=null){
				for(int i=0; i<list.Count; i++){
					if(list[i]!=null) return list[i];
				}
			}
			if(shootPointList!=null){
				for(int i=0; i<shootPointList.Count; i++){
					if(shootPointList[i]!=null) return shootPointList[i];
				}
			}
			return thisT;
		}

		private void InitAnimation(){
			if(animatorT!=null) animator=animatorT.GetComponent<Animator>();
			if(animator==null) return;
			
			//obsolete
			//AnimatorOverrideController aniOverrideController = new AnimatorOverrideController();
			//aniOverrideController.runtimeAnimatorController = animator.runtimeAnimatorController;
			//animator.runtimeAnimatorController = aniOverrideController;
			
			AnimatorOverrideController aniOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
			animator.runtimeAnimatorController = aniOverrideController;
			
			if(clipIdle!=null) 				aniOverrideController["Idle"] = clipIdle;
			if(clipMove!=null) 			aniOverrideController["Move"] = clipMove;
			if(clipHit!=null) 				aniOverrideController["Hit"] = clipHit;
			if(clipAttackRange!=null) 	aniOverrideController["AttackRange"] = clipAttackRange;
			if(clipAttackMelee!=null) 	aniOverrideController["AttackMelee"] = clipAttackMelee;
			if(clipDestroyed!=null) 	aniOverrideController["Destroyed"] = clipDestroyed;
			if(clipStunned!=null) 		aniOverrideController["Stunned"] = clipStunned;
			
			EnsureAbilityShootPointSetList();
			for(int i=0; i<Mathf.Min(6, clipAbilityList.Count); i++){
				if(clipAbilityList[i]==null) continue;
				aniOverrideController["Ability"+(i+1)] = clipAbilityList[i];
			}
			
			UpdateStunAnimation();
		}
		
		public void UpdateStunAnimation(){
			if(!useStunAnimation) return;
			if(animator==null) return;
			if(string.IsNullOrEmpty(stunAnimatorBool)) return;
			animator.SetBool(stunAnimatorBool, IsStunned());
		}
		
		private float AnimPlayAbility(int idx){
			Debug.Log("AnimPlayAbility  "+idx);
			if(animator==null || idx<0 || clipAbilityList.Count<=idx || clipAbilityList[idx]==null) return 0;

			animator.SetTrigger("Ability"+(idx+1));

			// Ability1 / Ability2 / Ability3 etc must be allowed to finish before JRPG melee return.
			// The returned value below is only the hit/shoot delay, not the full animation length.
			// So we also start the same wait flag used by AttackRange/AttackMelee.
			StartCoroutine(WaitingForAttackAnimation(clipAbilityList[idx].length));

			return animAbilityDelayList.Count>idx ? animAbilityDelayList[idx] : 0;
		}
		
		private void AnimPlayMove(bool moving){ 
			if(animator!=null) animator.SetBool("Moving", moving);
		}
		private void AnimPlayHit(){
			if(animator!=null && clipHit!=null) animator.SetTrigger("Hit");
		}
		private float AnimPlayDestroyed(){
			if(animator==null || clipDestroyed==null) return 0;
			if(clipDestroyed!=null) animator.SetBool("Destroyed", true);
			return clipDestroyed!=null ? clipDestroyed.length : 0 ;
		}
		private float AnimPlayAttack(bool isMelee){
			if(isMelee){
				if(animator==null || clipAttackMelee==null) return 0;
				if(clipAttackMelee!=null){
					animator.SetTrigger("AttackMelee");
					StartCoroutine(WaitingForAttackAnimation(clipAttackMelee.length));
				}
				return animAttackDelayMelee;
			}
			else{
				if(animator==null || clipAttackRange==null) return 0;
				if(clipAttackRange!=null){
					animator.SetTrigger("AttackRange");
					StartCoroutine(WaitingForAttackAnimation(clipAttackRange.length));
				}
				return animAttackDelayRange;
			}
		}
		
		private bool waitingForAttackAnimation=false;
		IEnumerator WaitingForAttackAnimation(float duration){
			waitingForAttackAnimation=true;
			yield return new WaitForSeconds(duration);
			waitingForAttackAnimation=false;
		}
		
		
		/*
		void CheckCover(){
			if(node.HasCover()>0){	//check if the node has any cover
				for(int i=0; i<4; i++){	//check for cover in each direction
					int cover=node.GetCover(i*90);
					if(cover==1){
						Debug.Log("unit has a half cover at "+(i*90)+" degree");
						animator.SetInteger("InCover", 1);
						
						//rotate unit to that direction is necessary
					}
					else if(cover==2){
						Debug.Log("unit has a full cover at "+(i*90)+" degree");
						animator.SetInteger("InCover", 2);
						
						//rotate unit to that direction is necessary
					}
					else{
						Debug.Log("unit has no cover");
						animator.SetInteger("InCover", 0);
					}
				}
			}
			else{
				Debug.Log("unit has no cover");
				animator.SetInteger("InCover", 0);
			}
		}
		*/
		
		
		//private int attackCounter=0;
		#endregion
		
		
		#region animation
		[Header("Audio")]
		public bool loopMoveSound=false;
		private AudioSource audioSrc;
		
		public AudioClip selectSound;
		public AudioClip moveSound;
		public AudioClip attackRangeSound;
		public AudioClip attackMeleeSound;
		public AudioClip hitSound;
		public AudioClip destroySound;
		
		
		private void InitAudio(){
			if(!loopMoveSound || audioSrc!=null) return;
			
			audioSrc=gameObject.AddComponent<AudioSource>();
			audioSrc.playOnAwake=false; audioSrc.loop=true; audioSrc.volume=1; //src.spatialBlend=.75f;
			audioSrc.clip=moveSound;
		}
		private void AudioPlayMove(){
			if(moveSound==null) return;
			
			if(loopMoveSound){
				InitAudio();		
				audioSrc.Play();
				return;
			}
			else AudioManager.PlaySound(moveSound, GetPos());
		}
		private void AudioStopMove(){
			if(audioSrc!=null && moveSound!=null) audioSrc.Stop();
		}
		
		
		
		public void AudioPlaySelect(){
			if(selectSound!=null) AudioManager.PlaySound(selectSound, GetPos());
		}
		public void AudioPlayAttack(bool isMelee){
			if(!isMelee && attackRangeSound!=null) AudioManager.PlaySound(attackRangeSound, GetPos());
			else if(attackMeleeSound!=null) AudioManager.PlaySound(attackMeleeSound, GetPos());
		}
		public void AudioPlayHit(){
			if(hitSound!=null) AudioManager.PlaySound(hitSound, GetPos());
		}
		public float AudioPlayDestroy(){
			if(destroySound==null) return 0;
			AudioManager.PlaySound(destroySound, GetPos());
			return destroySound.length;
		}
		#endregion
		
	}
	
	
}

