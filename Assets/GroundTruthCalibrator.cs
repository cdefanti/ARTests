using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using MathNet.Numerics.LinearAlgebra.Factorization;

public class GroundTruthCalibrator : MonoBehaviour {

    public Tracker[] trackers;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // Least-Fitting Squares of Two 3D Point Sets
        // http://post.queensu.ca/~sdb2/PAPERS/PAMI-3DLS-1987.pdf
        // Some ideas simplified to account for lack of roll/pitch drift

        Quaternion q = Quaternion.Inverse(transform.rotation);
        Vector3 t = -transform.position;

        // First, we calculate the centroid

        // number of visible trackers
        int nTracked = 0;
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
                nTracked++;
            }
        }
        // we need at least 3 points for this algorithm to work.
        if (nTracked < 3)
        {
            return;
        }
        rc /= nTracked;
        oc /= nTracked;

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
        // X = VU^T
        Matrix<float> X = svd.VT.Transpose() * svd.U.Transpose();
        // If det(X) = +1, optimal rotation = X
        // if det(X) = -1, the algorithm fails (bad data or degenerate cases)
        if (X.Determinant() > 0)
        {
            Vector3 forward = new Vector3(X[0, 2], X[1, 2], X[2, 2]);

            q = Quaternion.LookRotation(forward);
            t = rc - q * oc;
        } else
        {
            // no solution
        }

        transform.position = -t;
        transform.rotation = Quaternion.Inverse(q);
    }
}
