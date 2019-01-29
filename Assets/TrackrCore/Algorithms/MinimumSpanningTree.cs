
class MininumSpanningTree {

    void construct() {

    }

    void forward() {

    }

    byte acquireNextId() {
        return 0;
    }



}
/*
// root
do {
    for n in nbr {
        send(n, <d= 0, parent= false >)
    }
} while (true);

// At process n
do {
    n[i], parent = recv(nbr i)
    d = min(n) + 1
    parent = find(i s.t.n[i] = d - 1)
    send(parent, <d= d, parent= true >)
    for n in nbr {
        if n!= parent {
            send(n, <d = d, parent= false >)
        }
    }
} while (true);
*/