﻿using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {
    public const float DELAY_NEXT_SHOT = .2f;

    public GameObject TestLevel;
    public Level currentLevel;

    public float mouseSensitivity;

    private int _currentPlane = 0;
    private float _positionOnPlane = 0.5f; // between 0 (beginning) and 1 (end)
    
    private bool _alive = false;
    public int maxShots = 5;
    private int _numShots = 0;
    private float _reload = 0;

    public GameObject playerProjectile;

    // Use this for initialization
    void Start () {
        if (TestLevel != null) {
            currentLevel = TestLevel.GetComponent<Level>();
            init(currentLevel);
        }
        if (mouseSensitivity < .1f) {
            mouseSensitivity = .1f;
        }
        gameObject.rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void init(Level level) {
        currentLevel = level;
        _alive = true;
    }

    // Update is called once per frame
    void Update() {
        if (!_alive) {
            return;
        }
        float mouseMove = Input.GetAxis("Mouse X");
        float shipMove = mouseMove * mouseSensitivity;
        _positionOnPlane += shipMove;

        // calculate new position after movement
        if (_positionOnPlane < 0) {
            _currentPlane--;
            _positionOnPlane++;
        }
        else if (_positionOnPlane > 1) {
            _currentPlane++;
            _positionOnPlane--;
        }

        if (_currentPlane < 0) {
            if (currentLevel.wrapAround) {
                _currentPlane += currentLevel.lanes.Count;
            }
            else {
                _currentPlane = 0;
                _positionOnPlane = 0;
            }
        }
        else if (_currentPlane >= currentLevel.lanes.Count) {
            if (currentLevel.wrapAround) {
                _currentPlane -= currentLevel.lanes.Count;
            }
            else {
                _currentPlane = currentLevel.lanes.Count - 1;
                _positionOnPlane = 1;
            }
        }

        //print("Plane: " + _currentPlane + ", Position: " + _positionOnPlane);

        // update position
        transform.position = currentLevel.lanes[_currentPlane].Front;
        float angleUp = Vector3.Angle(Vector3.up, currentLevel.lanes[_currentPlane].Normal) - 90;
        float angleRight = Vector3.Angle(Vector3.forward, currentLevel.lanes[_currentPlane].Normal);
        float angleLeft = Vector3.Angle(Vector3.back, currentLevel.lanes[_currentPlane].Normal);
        if (angleRight < angleLeft) {
            angleUp = -angleUp;
        }
        transform.eulerAngles = new Vector3(angleUp, 180, 0);

        // shoot
        if (Input.GetMouseButton(0) && _reload < 0 && _numShots < maxShots) {
            Shoot();
            _reload = DELAY_NEXT_SHOT;
        
        }
        
        // reload
        _reload -= Time.deltaTime;
    }

    void Shoot() {
        _numShots++;
        GameObject shot = (GameObject)Instantiate(playerProjectile);
        PlayerProjectile shotScript = shot.GetComponent<PlayerProjectile>();
        shotScript.player = this;
        Lane currentLane = currentLevel.lanes[_currentPlane];
        Vector3 start = currentLane.Front + ((gameObject.renderer.bounds.size.y / 2) * currentLane.Normal);
        shotScript.init(currentLevel.lanes[_currentPlane]);
        shotScript.startingLocation = start;
        shot.transform.position = start;
    }
    
    public void RemoveShot() {
        _numShots--;
    }
    
    void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Enemy") {
            GameManager.Instance.removeShip(this);
            Destroy(gameObject);
        }
    }
}
