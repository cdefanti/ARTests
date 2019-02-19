using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Factorization;


public class TrackerGroup : MonoBehaviour {

    private Tracker[] trackers;
    private bool tracked;

    // position and rotation of the multitracker group that we observe
    protected Vector3 relPos;
    protected Quaternion relRot;

	// Use this for initialization
	void Start () {
        trackers = GetComponentsInChildren<Tracker>();
	}
	
	// Update is called once per frame
	void Update () {
        // Least-Fitting Squares of Two 3D Point Sets
        // http://post.queensu.ca/~sdb2/PAPERS/PAMI-3DLS-1987.pdf
        // Some ideas simplified to account for lack of roll/pitch drift

        Quaternion q = transform.rotation;
        Vector3 t = transform.position;
        tracked = false;

        // First, we calculate the centroid

        // visible trackers
        List<Tracker> visibleTrackers = new List<Tracker>();
        // real centroid
        Vector3 rc = Vector3.zero;
        // observed centroid
        Vector3 oc = Vector3.zero;
        foreach (Tracker tracker in trackers)
        {
            if (tracker.tracked)
            {
                rc += tracker.realPos;
                oc += tracker.transform.position;
                visibleTrackers.Add(tracker);
            }
        }

        int nTracked = visibleTrackers.Count;

        // we need at least 3 points for SVD algorithm to work.
        // if there are 1 or 2, we use the average
        if (nTracked == 0)
        {
            return;
        } else
        {
            rc /= nTracked;
            oc /= nTracked;
        }
        if (nTracked < 3)
        {
            relPos = oc;
            if (nTracked == 1)
            {
                relRot = visibleTrackers[0].transform.rotation;
            } else
            {
                relRot = Quaternion.Slerp(visibleTrackers[0].transform.rotation, visibleTrackers[1].transform.rotation, 0.5f);
            }

            return;
        }

        // Next, we calculate H matrix
        Matrix<float> H = DenseMatrix.Create(3, 3, 0f);
        foreach (Tracker tracker in trackers)
        {
            if (tracker.tracked)
            {
                Vector3 rq = tracker.realPos - rc;
                Vector3 oq = tracker.transform.position - oc;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        H[i, j] += rq[i] * oq[j];
                    }
                }
            }
        }

        // Get SVD of H, H = UDV^T
        Svd<float> svd = H.Svd(true);
        svd.Solve(H);
        // X = VU^T (= (UV^T)^T)
        Matrix<float> X = (svd.U * svd.VT).Transpose();
        // If det(X) = +1, optimal rotation = X
        // if det(X) = -1, either degenerate case or reflection
        if (X.Determinant() == -1)
        {
            int flip_index = -1;
            for (int i = 0; i < 3; i++)
            {
                if (svd.W[i, i] == 0)
                {
                    flip_index = i;
                    break;
                }
            }
            // VT' flips the row where the corresponding singular value is 0
            Matrix<float> VTp = DenseMatrix.Create(3, 3, 0f);
            svd.VT.CopyTo(VTp);
            VTp[flip_index, 0] = -VTp[flip_index, 0];
            VTp[flip_index, 1] = -VTp[flip_index, 1];
            VTp[flip_index, 2] = -VTp[flip_index, 2];

            X = (svd.U * VTp).Transpose();
        }

        Vector3 forward = new Vector3(X[0, 2], X[1, 2], X[2, 2]);

        q = Quaternion.LookRotation(forward);
        t = oc - q * rc;
        tracked = true;

        transform.position = t;
        transform.rotation = q;
    }
}
