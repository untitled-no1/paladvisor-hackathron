// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using HoloToolkit.Unity;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using System;
using System.Collections;
using System.Text;

namespace HoloToolkit.Examples.GazeRuler
{
    /// <summary>
    /// mananger all lines in the scene
    /// </summary>
    public class LineManager : Singleton<LineManager>, IGeometry
    {
        // save all lines in scene
        private Stack<Line> Lines = new Stack<Line>();

        private Point lastPoint;

        private const float defaultLineScale = 0.005f;
        private IEnumerator enumerator;

        // place point and lines
        public void AddPoint(GameObject LinePrefab, GameObject PointPrefab, GameObject TextPrefab)
        {

            Vector3 hitPoint = GazeManager.Instance.HitPosition;

            GameObject point = (GameObject)Instantiate(PointPrefab, hitPoint, Quaternion.identity);
            if (lastPoint != null && lastPoint.IsStart)
            {
                Vector3 centerPos = (lastPoint.Position + hitPoint) * 0.5f;

                Vector3 directionFromCamera = centerPos - Camera.main.transform.position;

                float distanceA = Vector3.Distance(lastPoint.Position, Camera.main.transform.position);
                float distanceB = Vector3.Distance(hitPoint, Camera.main.transform.position);

                Debug.Log("A: " + distanceA + ",B: " + distanceB);
                Vector3 direction;
                if (distanceB > distanceA || (distanceA > distanceB && distanceA - distanceB < 0.1))
                {
                    direction = hitPoint - lastPoint.Position;
                }
                else
                {
                    direction = lastPoint.Position - hitPoint;
                }

                float distance = Vector3.Distance(lastPoint.Position, hitPoint);
                GameObject line = (GameObject)Instantiate(LinePrefab, centerPos, Quaternion.LookRotation(direction));
                line.transform.localScale = new Vector3(distance, defaultLineScale, defaultLineScale);
                line.transform.Rotate(Vector3.down, 90f);

                Vector3 normalV = Vector3.Cross(direction, directionFromCamera);
                Vector3 normalF = Vector3.Cross(direction, normalV) * -1;
                GameObject tip = (GameObject)Instantiate(TextPrefab, centerPos, Quaternion.LookRotation(normalF));

                //unit is meter
                tip.transform.Translate(Vector3.up * 0.05f);
                tip.GetComponent<TextMesh>().text = distance + "m";

                GameObject root = new GameObject();
                lastPoint.Root.transform.parent = root.transform;
                line.transform.parent = root.transform;
                point.transform.parent = root.transform;
                tip.transform.parent = root.transform;

                Lines.Push(new Line
                {
                    Start = lastPoint.Position,
                    End = hitPoint,
                    Root = root,
                    Distance = distance
                });

                lastPoint = new Point
                {
                    Position = hitPoint,
                    Root = point,
                    IsStart = false
                };
                // custom code here
                Debug.Log("1 current distance" + distance);

                if (enumerator == null)
                {
                    enumerator = SendRequest(distance);
                }
                enumerator.MoveNext();
            }
            else
            {
                lastPoint = new Point
                {
                    Position = hitPoint,
                    Root = point,
                    IsStart = true
                };
            }
        }

        // delete latest placed lines
        public void Delete()
        {
            if (Lines != null && Lines.Count > 0)
            {
                Line lastLine = Lines.Pop();
                Destroy(lastLine.Root);
            }

        }

        // delete all lines in the scene
        public void Clear()
        {
            if (Lines != null && Lines.Count > 0)
            {
                while (Lines.Count > 0)
                {
                    Line lastLine = Lines.Pop();
                    Destroy(lastLine.Root);
                }
            }
        }

        // reset current unfinished line
        public void Reset()
        {
            if (lastPoint != null && lastPoint.IsStart)
            {
                Destroy(lastPoint.Root);
                lastPoint = null;
            }
        }


        private int cnt = 0;

        /// <summary>
        /// CUSTOM CODE
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>

        public IEnumerator SendRequest(float distance)
        {
            var destination = new Destination();
            Debug.Log("Destination add width!");
            yield return destination.width = distance;
            Debug.Log("Destination add length!");
            yield return destination.length = distance;
            Debug.Log("Destination add height!");
            yield return destination.height = distance;
            var list = new List<Gate>();
            Gate gate = null;
            //while (distance > 0)
            while (cnt < 1) // TODO: change with menu
            {
                gate = new Gate();
                Debug.Log("Gate add width!");
                yield return gate.width = distance;
                Debug.Log("Gate add height!");
                yield return gate.height = distance;
                list.Add(gate);
                cnt++;
            }
            cnt = 0;
            Debug.Log("Destination add gates!");
            destination.gates = list.ToArray();

            Debug.Log("Serialize json!");
            var json = JsonUtility.ToJson(destination);

            Debug.Log("Preparing rest call!");
            POST("http://paladvisor.azurewebsites.net/api/rest", json, () =>
            {
                Debug.Log("Sending was successful!");
            });

            enumerator = null;
        }
        
        private WWW POST(string url, string json, Action onComplete)
        {
            WWW www;
            var postHeader = new Dictionary<string, string>();
            postHeader.Add("Content-Type", "application/json");

            var formData = Encoding.UTF8.GetBytes(json);

            www = new WWW(url, formData, postHeader);

            Debug.Log("Starting Send Coroutine!");
            StartCoroutine(WaitForRequest(www, onComplete));
            return www;
        }

        private IEnumerator WaitForRequest(WWW www, System.Action onComplete)
        {
            yield return www;
            // check for errors
            if (www.error == null)
            {
                Debug.Log("POST response> " + www.text);
                onComplete();
            }
            else
            {
                Debug.Log(www.error);
            }
        }

    }


    public struct Line
    {
        public Vector3 Start { get; set; }

        public Vector3 End { get; set; }

        public GameObject Root { get; set; }

        public float Distance { get; set; }
    }


}