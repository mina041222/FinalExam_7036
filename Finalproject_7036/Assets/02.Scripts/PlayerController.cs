using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    static public bool isActivated = true;

    // 스피드 조정 변수
    [SerializeField]
    private float walkSpeed;          //걷기 스피드 private으로 선언
    [SerializeField]
    private float runSpeed;           //달리기 스피드 private으로 선언
    [SerializeField]
    private float crouchSpeed;

    private float applySpeed;

    [SerializeField]
    private float jumpForce;


    // 상태 변수
    private bool isWalk = false;
    private bool isRun = false;
    private bool isCrouch = false;
    private bool isGround = true;


    // 움직임 체크 변수
    private Vector3 lastPos;


    // 앉았을 때 얼마나 앉을지 결정하는 변수.
    [SerializeField]
    private float crouchPosY;
    private float originPosY;
    private float applyCrouchPosY;

    // 땅 착지 여부
    private CapsuleCollider capsuleCollider;


    // 민감도을 선언
    [SerializeField]
    private float lookSensitivity;  


    //카메라 돌리는거의 한계를 선언 
    [SerializeField]
    private float cameraRotationLimit;
    private float currentCameraRotationX = 0;


    //필요한 컴포넌트을 선언
    [SerializeField]
    private Camera theCamera; //PLAYER에는 카메라 컴포넌트가 없고 자식계채에 있다
    private Rigidbody myRigid;
    private GunController theGunController;
    private Crosshair theCrosshair;
    private StatusController theStatusController;

	void Start () 
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        myRigid = GetComponent<Rigidbody>();                             //myRigid에 Rigidbody 넣기
        theGunController = FindObjectOfType<GunController>();
        theCrosshair = FindObjectOfType<Crosshair>();
        theStatusController = FindObjectOfType<StatusController>();

        // 초기화.
        applySpeed = walkSpeed;
        originPosY = theCamera.transform.localPosition.y;
        applyCrouchPosY = originPosY;
    }
	void Update () 
    {
        if (isActivated)
        {
            IsGround();
            TryJump();
            TryRun();
            TryCrouch();
            Move();
            MoveCheck();
            if (!Inventory.inventoryActivated)
            {
                CameraRotation();
                CharacterRotation();
            }
        }
	}

    // 앉기 시도
    private void TryCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Crouch();
        }
    }


    // 앉기 동작
    private void Crouch()
    {
        isCrouch = !isCrouch;
        theCrosshair.CrouchingAnimation(isCrouch);

        if (isCrouch)
        {
            applySpeed = crouchSpeed;
            applyCrouchPosY = crouchPosY;
        }
        else
        {
            applySpeed = walkSpeed;
            applyCrouchPosY = originPosY;
        }

        StartCoroutine(CrouchCoroutine());

    }


    // 부드러운 동작 실행.
    IEnumerator CrouchCoroutine()
    {

        float _posY = theCamera.transform.localPosition.y;
        int count = 0;

        while(_posY != applyCrouchPosY)
        {
            count++;
            _posY = Mathf.Lerp(_posY, applyCrouchPosY, 0.3f);
            theCamera.transform.localPosition = new Vector3(0, _posY, 0);
            if (count > 15)
                break;
            yield return null;
        }
        theCamera.transform.localPosition = new Vector3(0, applyCrouchPosY, 0f);
    }


    // 지면 체크.
    private void IsGround()
    {
        isGround = Physics.Raycast(transform.position, Vector3.down, capsuleCollider.bounds.extents.y + 0.1f);
        theCrosshair.JumpingAnimation(!isGround);
    }


    // 점프 시도
    private void TryJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGround && theStatusController.GetCurrentSP() > 0)
        {
            Jump();
        }
    }


    // 점프
    private void Jump()
    {

        // 앉은 상태에서 점프시 앉은 상태 해제.
        if (isCrouch)
            Crouch();
        theStatusController.DecreaseStamina(100);
        myRigid.velocity = transform.up * jumpForce;
    }


    // 달리기 시도
    private void TryRun()
    {
        if (Input.GetKey(KeyCode.LeftShift) && theStatusController.GetCurrentSP() > 0)
        {
            Running();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift) || theStatusController.GetCurrentSP() <= 0)
        {
            RunningCancel();
        }
    }


    // 달리기 실행
    private void Running()
    {
        if (isCrouch)
            Crouch();

        theGunController.CancelFineSight();

        isRun = true;
        theCrosshair.RunningAnimation(isRun);
        theStatusController.DecreaseStamina(10);
        applySpeed = runSpeed;
    }


    // 달리기 취소
    private void RunningCancel()
    {
        isRun = false;
        theCrosshair.RunningAnimation(isRun);
        applySpeed = walkSpeed;
    }


    // 움직임 실행
    private void Move()
    {
        float _moveDirX = Input.GetAxisRaw("Horizontal");   //키보드 A,D나 좌우 방향키를 누르면  -1, 1, 0 으로 눌러짐을 선언
        float _moveDirZ = Input.GetAxisRaw("Vertical");     // 키보드 W,S나 상하 방향키를 누르면 -1, 1, 0으로 눌러짐을 선언

        //좌우상하 구분 가능하게 하는 문장
        Vector3 _moveHorizontal = transform.right * _moveDirX;     // transform.right * _moveDirX  == (1, 0 , 0)  * 1
        Vector3 _moveVertical = transform.forward * _moveDirZ;     //transform.forward * _moveDirZ  = (0 ,0, 1) * 1

        Vector3 _velocity = (_moveHorizontal + _moveVertical).normalized * applySpeed;   //transform.right + transoform.forward
                                                                                         //(1, 0, 0 )+ (0, 0, 1) = (1, 0, 1)
                                                                                         // 함수의 법칙을 이용해서 (1, 0, 1) = (0.5, 0 , 0.5)이기 때문에 간단하게 normalized를 쓰는거다
                                                                                         //방향을 이용했기에 speed도 쓴다
                                                                                         

        myRigid.MovePosition(transform.position + _velocity * Time.deltaTime);          // 현재 위치 + _velocity값을 한 프레임만큼 움직이는거)
    }


    // 움직임 체크
    private void MoveCheck()
    {
        if (!isRun && !isCrouch && isGround)
        {
            if (Vector3.Distance(lastPos, transform.position) >= 0.01f)
                isWalk = true;
            else
                isWalk = false;

            theCrosshair.WalkingAnimation(isWalk);
            lastPos = transform.position;
        }
    }


    // 좌우 캐릭터 회전
    private void CharacterRotation()
    {
        float _yRotation = Input.GetAxisRaw("Mouse X"); //마우스는 XY만 존재하고 X는 좌우를 뜻하므로 Mouse X로 선언한다
        Vector3 _characterRotationY = new Vector3(0f, _yRotation, 0f) * lookSensitivity;//상하와 좌우의 민감도를 같게 설정
        myRigid.MoveRotation(myRigid.rotation * Quaternion.Euler(_characterRotationY)); //vector3 값을 Quarternion으로 변환 , 즉 myRigid를 이용했으므로 그걸 이용해 케릭터를 음직이게 한다 
    }



    // 상하 카메라 회전
    private void CameraRotation()
    {
        if (!pauseCameraRotation)
        {
            float _xRotation = Input.GetAxisRaw("Mouse Y"); //마우스는 xy만 존재한다.x는 좌우 y는 상하이므로 Mouse Y는 상하를 의미한다
            float _cameraRotationX = _xRotation * lookSensitivity; //회전이 너무 빠르지 않지 않게 민감도를 설정해주는 것
            currentCameraRotationX -= _cameraRotationX;
            currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraRotationLimit, cameraRotationLimit); //CameraRotationLimit로 currentCameraRotation이 45도 이상 넘김을 방지해준다

            theCamera.transform.localEulerAngles = new Vector3(currentCameraRotationX, 0f, 0f);  //localEulerAngles는 유니티 안에서 XYZ이다
                                                                                                 
        }
    }

    private bool pauseCameraRotation = false;

    public IEnumerator TreeLookCoroutine(Vector3 _target)
    {
        pauseCameraRotation = true;

        Quaternion direction = Quaternion.LookRotation(_target - theCamera.transform.position);
        Vector3 eulerValue = direction.eulerAngles;
        float destinationX = eulerValue.x;

        while(Mathf.Abs(destinationX - currentCameraRotationX) >= 0.5f)
        {
            eulerValue = Quaternion.Lerp(theCamera.transform.localRotation, direction, 0.3f).eulerAngles;
            theCamera.transform.localRotation = Quaternion.Euler(eulerValue.x, 0f, 0f);
            currentCameraRotationX = theCamera.transform.localEulerAngles.x;
            yield return null;
        }

        pauseCameraRotation = false;
    }

    // 상태 변수 값 반환
    public bool GetRun()
    {
        return isRun;
    }
    public bool GetWalk()
    {
        return isWalk;
    }
    public bool GetCrouch()
    {
        return isCrouch;
    }
    public bool GetIsGround()
    {
        return isGround;
    }
}


